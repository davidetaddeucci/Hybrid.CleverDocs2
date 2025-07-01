-- Create Conversations and Messages tables for R2R Chatbot functionality
-- Database: cleverdocs (PostgreSQL)
-- Date: 2025-06-20

-- Update Conversations table to use correct UUID types
ALTER TABLE "Conversations"
    ALTER COLUMN "UserId" TYPE UUID USING "UserId"::text::UUID,
    ALTER COLUMN "CompanyId" TYPE UUID USING "CompanyId"::text::UUID;
    
    -- Foreign key constraints
    CONSTRAINT "FK_Conversations_Users_UserId" 
        FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Conversations_Companies_CompanyId" 
        FOREIGN KEY ("CompanyId") REFERENCES "Companies"("Id") ON DELETE CASCADE
);

-- Update Messages table to remove UserId and CompanyId columns (inherited from Conversation)
ALTER TABLE "Messages"
    DROP COLUMN IF EXISTS "UserId",
    DROP COLUMN IF EXISTS "CompanyId";

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS "IX_Conversations_UserId" ON "Conversations"("UserId");
CREATE INDEX IF NOT EXISTS "IX_Conversations_CompanyId" ON "Conversations"("CompanyId");
CREATE INDEX IF NOT EXISTS "IX_Conversations_R2RConversationId" ON "Conversations"("R2RConversationId");
CREATE INDEX IF NOT EXISTS "IX_Conversations_Status" ON "Conversations"("Status");
CREATE INDEX IF NOT EXISTS "IX_Conversations_CreatedAt" ON "Conversations"("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_Conversations_LastMessageAt" ON "Conversations"("LastMessageAt");

-- Update indexes (UserId and CompanyId removed from Messages)
CREATE INDEX IF NOT EXISTS "IX_Messages_ConversationId" ON "Messages"("ConversationId");
CREATE INDEX IF NOT EXISTS "IX_Messages_R2RMessageId" ON "Messages"("R2RMessageId");
CREATE INDEX IF NOT EXISTS "IX_Messages_Role" ON "Messages"("Role");
CREATE INDEX IF NOT EXISTS "IX_Messages_ParentMessageId" ON "Messages"("ParentMessageId");
CREATE INDEX IF NOT EXISTS "IX_Messages_CreatedAt" ON "Messages"("CreatedAt");

-- Create trigger to update UpdatedAt automatically
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply triggers
CREATE TRIGGER update_conversations_updated_at 
    BEFORE UPDATE ON "Conversations" 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_messages_updated_at 
    BEFORE UPDATE ON "Messages" 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Create trigger to update LastMessageAt in Conversations
CREATE OR REPLACE FUNCTION update_conversation_last_message_at()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE "Conversations" 
    SET "LastMessageAt" = NEW."CreatedAt"
    WHERE "Id" = NEW."ConversationId";
    RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TRIGGER update_conversation_last_message_at_trigger
    AFTER INSERT ON "Messages"
    FOR EACH ROW EXECUTE FUNCTION update_conversation_last_message_at();

-- Insert sample data for testing (optional)
-- INSERT INTO "Conversations" ("Title", "Description", "UserId", "CompanyId", "Settings")
-- VALUES 
--     ('Test Conversation', 'A test conversation for R2R chatbot', 
--      (SELECT "Id" FROM "Users" LIMIT 1), 
--      (SELECT "Id" FROM "Companies" LIMIT 1), 
--      '{"useVectorSearch": true, "useHybridSearch": true, "maxResults": 10}');

COMMIT;
