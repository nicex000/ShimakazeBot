using Microsoft.EntityFrameworkCore.Migrations;

namespace Shimakaze_chan.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildPrefix",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(nullable: false),
                    Prefix = table.Column<string>(maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildPrefix", x => x.GuildId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildPrefix");
        }
    }
}
