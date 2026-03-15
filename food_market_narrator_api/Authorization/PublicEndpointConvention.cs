using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;

namespace food_market_narrator_api.Authorization;

public sealed class PublicEndpointConvention : IApplicationModelConvention
{
    private readonly HashSet<string> _publicEndpointKeys;

    public PublicEndpointConvention(IEnumerable<PublicEndpointDefinition> definitions)
    {
        _publicEndpointKeys = definitions
            .Select(d => BuildKey(d.HttpMethod, NormalizeRoute(d.RouteTemplate)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public void Apply(ApplicationModel application)
    {
        foreach (ControllerModel controller in application.Controllers)
        {
            string controllerTemplate = controller.Selectors
                .FirstOrDefault(s => s.AttributeRouteModel is not null)?
                .AttributeRouteModel?
                .Template ?? string.Empty;

            foreach (ActionModel action in controller.Actions)
            {
                IEnumerable<HttpMethodAttribute> httpAttributes = action.Attributes.OfType<HttpMethodAttribute>();
                if (!httpAttributes.Any())
                {
                    continue;
                }

                foreach (HttpMethodAttribute httpAttribute in httpAttributes)
                {
                    string actionTemplate = httpAttribute.Template ?? string.Empty;
                    string combinedTemplate = CombineTemplates(controllerTemplate, actionTemplate);
                    string resolvedTemplate = ReplaceTokens(combinedTemplate, controller.ControllerName, action.ActionName);
                    string normalizedRoute = NormalizeRoute(resolvedTemplate);

                    bool isPublic = httpAttribute.HttpMethods.Any(method =>
                        _publicEndpointKeys.Contains(BuildKey(method, normalizedRoute)));

                    if (isPublic)
                    {
                        foreach (SelectorModel selector in action.Selectors)
                        {
                            selector.EndpointMetadata.Add(new AllowAnonymousAttribute());
                        }
                        action.Filters.Add(new AllowAnonymousFilter());
                        break;
                    }
                }
            }
        }
    }

    private static string BuildKey(string method, string route)
    {
        return $"{method.Trim().ToUpperInvariant()}:{route}";
    }

    private static string NormalizeRoute(string route)
    {
        string normalized = route.Trim();

        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        if (normalized.Length > 1)
        {
            normalized = normalized.TrimEnd('/');
        }

        return normalized;
    }

    private static string CombineTemplates(string controllerTemplate, string actionTemplate)
    {
        string left = controllerTemplate.Trim('/');
        string right = actionTemplate.Trim('/');

        if (string.IsNullOrWhiteSpace(left))
        {
            return string.IsNullOrWhiteSpace(right) ? "/" : "/" + right;
        }

        if (string.IsNullOrWhiteSpace(right))
        {
            return "/" + left;
        }

        return "/" + left + "/" + right;
    }

    private static string ReplaceTokens(string routeTemplate, string controllerName, string actionName)
    {
        return routeTemplate
            .Replace("[controller]", controllerName, StringComparison.OrdinalIgnoreCase)
            .Replace("[action]", actionName, StringComparison.OrdinalIgnoreCase);
    }
}
