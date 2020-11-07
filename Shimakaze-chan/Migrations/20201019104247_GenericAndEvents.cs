using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Shimakaze_chan.Migrations
{
    public partial class GenericAndEvents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShimaGeneric",
                columns: table => new
                {
                    Key = table.Column<string>(maxLength: 200, nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShimaGeneric", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "TimedEvents",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<int>(nullable: false),
                    EventTime = table.Column<DateTime>(nullable: false),
                    Message = table.Column<string>(maxLength: 2000, nullable: true),
                    ChannelId = table.Column<decimal>(nullable: false),
                    UserId = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimedEvents", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShimaGeneric");

            migrationBuilder.DropTable(
                name: "TimedEvents");
        }
    }
}
