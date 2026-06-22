using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SourceBase.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCaseInsensitiveCollation : Migration
    {
        private const string TargetLocale = "und-u-ks-level2";
        private const string OldLocale = "und-x-icu";
        private const string CollationName = "case_insensitive";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL does not support ALTER COLLATION. We collect all columns
            // using the old collation, drop it, recreate with the correct locale,
            // then re-apply. Uses a DO block so it is a no-op on fresh databases
            // that already have the correct locale from the Init migration.
            migrationBuilder.Sql($"""
                DO $$
                DECLARE
                    col_list jsonb := '[]'::jsonb;
                    r RECORD;
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_collation
                        WHERE collname = '{CollationName}'
                        AND colllocale = '{TargetLocale}'
                    ) THEN
                        RETURN;
                    END IF;

                    SELECT jsonb_agg(jsonb_build_object('s', table_schema, 't', table_name, 'c', column_name))
                    INTO col_list
                    FROM information_schema.columns
                    WHERE collation_name = '{CollationName}' AND table_schema = 'public';

                    FOR r IN SELECT * FROM jsonb_to_recordset(COALESCE(col_list, '[]'::jsonb)) AS x(s text, t text, c text)
                    LOOP
                        EXECUTE format('ALTER TABLE %I.%I ALTER COLUMN %I TYPE text', r.s, r.t, r.c);
                    END LOOP;

                    DROP COLLATION "{CollationName}";
                    EXECUTE $sql$CREATE COLLATION "{CollationName}" (provider = icu, locale = '{TargetLocale}', deterministic = false)$sql$;

                    FOR r IN SELECT * FROM jsonb_to_recordset(COALESCE(col_list, '[]'::jsonb)) AS x(s text, t text, c text)
                    LOOP
                        EXECUTE format('ALTER TABLE %I.%I ALTER COLUMN %I TYPE text COLLATE "{CollationName}"', r.s, r.t, r.c);
                    END LOOP;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                DO $$
                DECLARE
                    col_list jsonb := '[]'::jsonb;
                    r RECORD;
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_collation
                        WHERE collname = '{CollationName}'
                        AND colllocale = '{OldLocale}'
                    ) THEN
                        RETURN;
                    END IF;

                    SELECT jsonb_agg(jsonb_build_object('s', table_schema, 't', table_name, 'c', column_name))
                    INTO col_list
                    FROM information_schema.columns
                    WHERE collation_name = '{CollationName}' AND table_schema = 'public';

                    FOR r IN SELECT * FROM jsonb_to_recordset(COALESCE(col_list, '[]'::jsonb)) AS x(s text, t text, c text)
                    LOOP
                        EXECUTE format('ALTER TABLE %I.%I ALTER COLUMN %I TYPE text', r.s, r.t, r.c);
                    END LOOP;

                    DROP COLLATION "{CollationName}";
                    EXECUTE $sql$CREATE COLLATION "{CollationName}" (provider = icu, locale = '{OldLocale}', deterministic = false)$sql$;

                    FOR r IN SELECT * FROM jsonb_to_recordset(COALESCE(col_list, '[]'::jsonb)) AS x(s text, t text, c text)
                    LOOP
                        EXECUTE format('ALTER TABLE %I.%I ALTER COLUMN %I TYPE text COLLATE "{CollationName}"', r.s, r.t, r.c);
                    END LOOP;
                END $$;
                """);
        }
    }
}
