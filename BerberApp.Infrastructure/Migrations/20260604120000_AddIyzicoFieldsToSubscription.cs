using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BerberApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIyzicoFieldsToSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IyzicoPaymentId",
                table: "Subscriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IyzicoConversationId",
                table: "Subscriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardToken",
                table: "Subscriptions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardUserKey",
                table: "Subscriptions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_IyzicoConversationId",
                table: "Subscriptions",
                column: "IyzicoConversationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_IyzicoConversationId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(name: "IyzicoPaymentId",     table: "Subscriptions");
            migrationBuilder.DropColumn(name: "IyzicoConversationId", table: "Subscriptions");
            migrationBuilder.DropColumn(name: "CardToken",            table: "Subscriptions");
            migrationBuilder.DropColumn(name: "CardUserKey",          table: "Subscriptions");
        }
    }
}
