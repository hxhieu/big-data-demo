using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HDInsight.Migrations
{
    public partial class SecretClearText : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecretClearText",
                table: "AspNetUserOpenIddictApplications",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecretClearText",
                table: "AspNetUserOpenIddictApplications");
        }
    }
}
