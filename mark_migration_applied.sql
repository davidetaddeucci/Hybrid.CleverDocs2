-- Mark the Conversation and Message entities migration as applied
-- Since we created the tables manually, we need to mark the migration as applied

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250620122527_AddConversationAndMessageEntities', '9.0.6')
ON CONFLICT ("MigrationId") DO NOTHING;
