using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace unibucGram.Migrations
{
    /// <inheritdoc />
    public partial class GroupsIsDMChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDirectMessage",
                table: "Groups",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDirectMessage",
                table: "Groups");
        }
    }
}
