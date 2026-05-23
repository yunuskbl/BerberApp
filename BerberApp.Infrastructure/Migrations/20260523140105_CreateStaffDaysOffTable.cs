using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BerberApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateStaffDaysOffTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tablo zaten varsa atla (önceki boş migration'dan kalan durumlar için)
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""StaffDaysOff"" (
                    ""Id"" uuid NOT NULL,
                    ""StaffId"" uuid NOT NULL,
                    ""Date"" date NOT NULL,
                    ""Reason"" text,
                    ""CreatedAt"" timestamp without time zone NOT NULL,
                    ""UpdatedAt"" timestamp without time zone,
                    ""IsDeleted"" boolean NOT NULL DEFAULT false,
                    CONSTRAINT ""PK_StaffDaysOff"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_StaffDaysOff_Staff_StaffId"" FOREIGN KEY (""StaffId"")
                        REFERENCES ""Staff"" (""Id"") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ""IX_StaffDaysOff_StaffId"" ON ""StaffDaysOff"" (""StaffId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "StaffDaysOff");
        }
    }
}
