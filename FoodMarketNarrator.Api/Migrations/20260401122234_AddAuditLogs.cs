using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace food_market_narrator_api.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    target_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    target_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    target_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ip_address = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    language_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    language_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    language_code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.language_id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "Restaurant",
                columns: table => new
                {
                    restaurant_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    latitude = table.Column<decimal>(type: "decimal(10,6)", nullable: true),
                    longitude = table.Column<decimal>(type: "decimal(10,6)", nullable: true),
                    address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    open_time = table.Column<TimeSpan>(type: "time", nullable: true),
                    close_time = table.Column<TimeSpan>(type: "time", nullable: true),
                    user_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Restaurant", x => x.restaurant_id);
                    table.ForeignKey(
                        name: "FK_Restaurant_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Audio",
                columns: table => new
                {
                    audio_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    restaurant_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    language_id = table.Column<int>(type: "int", nullable: false),
                    audio_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    date_generation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Audio", x => x.audio_id);
                    table.ForeignKey(
                        name: "FK_Audio_Languages_language_id",
                        column: x => x.language_id,
                        principalTable: "Languages",
                        principalColumn: "language_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Audio_Restaurant_restaurant_id",
                        column: x => x.restaurant_id,
                        principalTable: "Restaurant",
                        principalColumn: "restaurant_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Restaurant_Image",
                columns: table => new
                {
                    image_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    restaurant_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    is_primary = table.Column<bool>(type: "bit", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Restaurant_Image", x => x.image_id);
                    table.ForeignKey(
                        name: "FK_Restaurant_Image_Restaurant_restaurant_id",
                        column: x => x.restaurant_id,
                        principalTable: "Restaurant",
                        principalColumn: "restaurant_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dish",
                columns: table => new
                {
                    dish_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    price = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    restaurant_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: true),
                    image_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dish", x => x.dish_id);
                    table.ForeignKey(
                        name: "FK_Dish_Restaurant_Image_image_id",
                        column: x => x.image_id,
                        principalTable: "Restaurant_Image",
                        principalColumn: "image_id");
                    table.ForeignKey(
                        name: "FK_Dish_Restaurant_restaurant_id",
                        column: x => x.restaurant_id,
                        principalTable: "Restaurant",
                        principalColumn: "restaurant_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Audio_language_id",
                table: "Audio",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "IX_Audio_restaurant_id",
                table: "Audio",
                column: "restaurant_id");

            migrationBuilder.CreateIndex(
                name: "IX_Dish_image_id",
                table: "Dish",
                column: "image_id");

            migrationBuilder.CreateIndex(
                name: "IX_Dish_restaurant_id",
                table: "Dish",
                column: "restaurant_id");

            migrationBuilder.CreateIndex(
                name: "IX_Restaurant_user_id",
                table: "Restaurant",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Restaurant_Image_restaurant_id",
                table: "Restaurant_Image",
                column: "restaurant_id");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_created_at",
                table: "AuditLogs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_user_id",
                table: "AuditLogs",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Audio");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Dish");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "Restaurant_Image");

            migrationBuilder.DropTable(
                name: "Restaurant");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_created_at",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_user_id",
                table: "AuditLogs");
        }
    }
}
