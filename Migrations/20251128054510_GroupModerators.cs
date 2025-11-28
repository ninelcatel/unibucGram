using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace unibucGram.Migrations
{
    /// <inheritdoc />
    public partial class GroupModerators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isModerator",
                table: "GroupMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isModerator",
                table: "GroupMembers");
        }
    }
}
