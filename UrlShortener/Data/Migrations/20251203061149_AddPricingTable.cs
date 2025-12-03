using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UrlShortener.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FAQs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Question = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FAQs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IconClass = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PricingPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    YearlyPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    IsPopular = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RecommendedFor = table.Column<string>(type: "text", nullable: false),
                    ButtonText = table.Column<string>(type: "text", nullable: false),
                    ButtonCssClass = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IconClass = table.Column<string>(type: "text", nullable: false),
                    ColorClass = table.Column<string>(type: "text", nullable: false),
                    FeatureCategoryId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureItem_FeatureCategories_FeatureCategoryId",
                        column: x => x.FeatureCategoryId,
                        principalTable: "FeatureCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PricingFeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    IconClass = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    PricingPlanId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PricingFeatures_PricingPlans_PricingPlanId",
                        column: x => x.PricingPlanId,
                        principalTable: "PricingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureItem_FeatureCategoryId",
                table: "FeatureItem",
                column: "FeatureCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingFeatures_PricingPlanId",
                table: "PricingFeatures",
                column: "PricingPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FAQs");

            migrationBuilder.DropTable(
                name: "FeatureItem");

            migrationBuilder.DropTable(
                name: "PricingFeatures");

            migrationBuilder.DropTable(
                name: "FeatureCategories");

            migrationBuilder.DropTable(
                name: "PricingPlans");
        }
    }
}
