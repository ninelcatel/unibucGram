using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace unibucGram.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MediaType",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoURL",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoURL",
                table: "Posts");
        }
    }
}
