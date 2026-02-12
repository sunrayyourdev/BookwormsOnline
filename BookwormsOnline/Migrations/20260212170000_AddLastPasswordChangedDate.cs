using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookwormsOnline.Migrations
{
    /// <inheritdoc />
    public partial class AddLastPasswordChangedDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastPasswordChangedDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPasswordChangedDate",
                table: "AspNetUsers");
        }
    }
}