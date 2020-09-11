using Microsoft.EntityFrameworkCore.Migrations;

namespace Shimakaze_chan.Migrations
{
    public partial class SelfAssignRoleLimit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildSelfAssign",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(nullable: false),
                    RoleId = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildSelfAssign", x => x.GuildId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildSelfAssign");
        }
    }
}
