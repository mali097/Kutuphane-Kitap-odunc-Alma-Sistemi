using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibrarySystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class EnsureBookRatingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[BookRatings]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [BookRatings] (
                        [Id] int NOT NULL IDENTITY,
                        [UserId] int NOT NULL,
                        [BookId] int NOT NULL,
                        [Score] decimal(3,1) NOT NULL,
                        [CreatedDate] datetime2 NOT NULL,
                        [CreatedBy] int NOT NULL,
                        [UpdatedDate] datetime2 NULL,
                        [UpdatedBy] int NULL,
                        [IsDeleted] bit NOT NULL,
                        CONSTRAINT [PK_BookRatings] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_BookRatings_Books_BookId] FOREIGN KEY ([BookId]) REFERENCES [Books] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_BookRatings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_BookRatings_BookId] ON [BookRatings] ([BookId]);
                    CREATE UNIQUE INDEX [IX_BookRatings_UserId_BookId] ON [BookRatings] ([UserId], [BookId]);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BookRatings");
        }
    }
}
