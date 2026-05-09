using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwimRadar.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoPreviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreviewError",
                table: "SwimVideos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviewFilePath",
                table: "SwimVideos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreviewGeneratedAt",
                table: "SwimVideos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreviewStatus",
                table: "SwimVideos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreviewError",
                table: "SwimVideos");

            migrationBuilder.DropColumn(
                name: "PreviewFilePath",
                table: "SwimVideos");

            migrationBuilder.DropColumn(
                name: "PreviewGeneratedAt",
                table: "SwimVideos");

            migrationBuilder.DropColumn(
                name: "PreviewStatus",
                table: "SwimVideos");
        }
    }
}
