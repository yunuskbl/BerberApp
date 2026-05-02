using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BerberApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""TenantPhotos"" (
                    ""Id""        uuid NOT NULL DEFAULT gen_random_uuid(),
                    ""TenantId""  uuid NOT NULL,
                    ""Url""       text NOT NULL,
                    ""Order""     integer NOT NULL DEFAULT 0,
                    ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" timestamp without time zone NULL,
                    ""IsDeleted"" boolean NOT NULL DEFAULT false,
                    CONSTRAINT ""PK_TenantPhotos"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_TenantPhotos_Tenants_TenantId"" FOREIGN KEY (""TenantId"")
                        REFERENCES ""Tenants"" (""Id"") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ""IX_TenantPhotos_TenantId"" ON ""TenantPhotos"" (""TenantId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TenantPhotos");
        }
    }
}
