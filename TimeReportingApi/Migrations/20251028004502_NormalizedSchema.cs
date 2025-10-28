using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TimeReportingApi.Migrations
{
    /// <inheritdoc />
    public partial class NormalizedSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "project_tasks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    task_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_tasks", x => x.id);
                    table.ForeignKey(
                        name: "FK_project_tasks_projects_project_code",
                        column: x => x.project_code,
                        principalTable: "projects",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tag_configurations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    tag_name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tag_configurations", x => x.id);
                    table.ForeignKey(
                        name: "FK_tag_configurations_projects_project_code",
                        column: x => x.project_code,
                        principalTable: "projects",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "time_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    project_task_id = table.Column<int>(type: "integer", nullable: false),
                    issue_id = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    standard_hours = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    overtime_hours = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    completion_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    decline_comment = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_time_entries", x => x.id);
                    table.CheckConstraint("chk_date_range", "start_date <= completion_date");
                    table.CheckConstraint("chk_overtime_hours_positive", "overtime_hours >= 0");
                    table.CheckConstraint("chk_standard_hours_positive", "standard_hours >= 0");
                    table.CheckConstraint("chk_status_valid", "status IN ('NOT_REPORTED', 'SUBMITTED', 'APPROVED', 'DECLINED')");
                    table.ForeignKey(
                        name: "FK_time_entries_project_tasks_project_task_id",
                        column: x => x.project_task_id,
                        principalTable: "project_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_time_entries_projects_project_code",
                        column: x => x.project_code,
                        principalTable: "projects",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tag_allowed_values",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tag_configuration_id = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tag_allowed_values", x => x.id);
                    table.ForeignKey(
                        name: "FK_tag_allowed_values_tag_configurations_tag_configuration_id",
                        column: x => x.tag_configuration_id,
                        principalTable: "tag_configurations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "time_entry_tags",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    time_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_allowed_value_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_time_entry_tags", x => x.id);
                    table.ForeignKey(
                        name: "FK_time_entry_tags_tag_allowed_values_tag_allowed_value_id",
                        column: x => x.tag_allowed_value_id,
                        principalTable: "tag_allowed_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_time_entry_tags_time_entries_time_entry_id",
                        column: x => x.time_entry_id,
                        principalTable: "time_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_project_tasks_project",
                table: "project_tasks",
                column: "project_code");

            migrationBuilder.CreateIndex(
                name: "uq_project_tasks_project_task",
                table: "project_tasks",
                columns: new[] { "project_code", "task_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_projects_active",
                table: "projects",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "uq_projects_name",
                table: "projects",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_tag_allowed_values_config",
                table: "tag_allowed_values",
                column: "tag_configuration_id");

            migrationBuilder.CreateIndex(
                name: "idx_tag_configurations_project",
                table: "tag_configurations",
                column: "project_code");

            migrationBuilder.CreateIndex(
                name: "uq_tag_configurations_project_tag",
                table: "tag_configurations",
                columns: new[] { "project_code", "tag_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_time_entries_project_date",
                table: "time_entries",
                columns: new[] { "project_code", "start_date" });

            migrationBuilder.CreateIndex(
                name: "idx_time_entries_status",
                table: "time_entries",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_time_entries_user",
                table: "time_entries",
                columns: new[] { "user_id", "start_date" });

            migrationBuilder.CreateIndex(
                name: "IX_time_entries_project_task_id",
                table: "time_entries",
                column: "project_task_id");

            migrationBuilder.CreateIndex(
                name: "idx_time_entry_tags_entry",
                table: "time_entry_tags",
                column: "time_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_time_entry_tags_tag_allowed_value_id",
                table: "time_entry_tags",
                column: "tag_allowed_value_id");

            migrationBuilder.CreateIndex(
                name: "uq_time_entry_tags_entry_value",
                table: "time_entry_tags",
                columns: new[] { "time_entry_id", "tag_allowed_value_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "time_entry_tags");

            migrationBuilder.DropTable(
                name: "tag_allowed_values");

            migrationBuilder.DropTable(
                name: "time_entries");

            migrationBuilder.DropTable(
                name: "tag_configurations");

            migrationBuilder.DropTable(
                name: "project_tasks");

            migrationBuilder.DropTable(
                name: "projects");
        }
    }
}
