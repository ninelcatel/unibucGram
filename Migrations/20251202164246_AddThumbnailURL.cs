using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace unibucGram.Migrations
{
    /// <inheritdoc />
    public partial class AddThumbnailURL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThumbnailURL",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThumbnailURL",
                table: "Posts");
        }
    }
}
