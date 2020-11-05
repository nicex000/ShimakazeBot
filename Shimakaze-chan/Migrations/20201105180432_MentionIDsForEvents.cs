using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Shimakaze_chan.Migrations
{
    public partial class MentionIDsForEvents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal[]>(
                name: "MentionRoleIdList",
                table: "TimedEvents",
                nullable: true);

            migrationBuilder.AddColumn<decimal[]>(
                name: "MentionUserIdList",
                table: "TimedEvents",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MentionRoleIdList",
                table: "TimedEvents");

            migrationBuilder.DropColumn(
                name: "MentionUserIdList",
                table: "TimedEvents");
        }
    }
}
