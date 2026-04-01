import "leaflet";

declare module "leaflet" {
  interface HeatLayerOptions extends LayerOptions {
    minOpacity?: number;
    maxZoom?: number;
    max?: number;
    radius?: number;
    blur?: number;
    gradient?: Record<number, string>;
  }

  interface HeatLayer extends Layer {
    setOptions(options: HeatLayerOptions): this;
    addLatLng(latlng: [number, number, number] | LatLngExpression): this;
    setLatLngs(
      latlngs: Array<[number, number, number] | LatLngExpression>,
    ): this;
    redraw(): this;
  }

  function heatLayer(
    latlngs: Array<[number, number, number] | LatLngExpression>,
    options?: HeatLayerOptions,
  ): HeatLayer;
}

declare module "leaflet.heat";
