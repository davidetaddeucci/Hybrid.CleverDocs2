# R2R Configuration Fix: Correct Model Names (o4-mini)

**Date**: January 9, 2025  
**Issue**: R2R configured with invalid model names causing chat fallback responses  
**Solution**: Update to correct `openai/o4-mini` model names  

## Current Invalid Configuration

**R2R System Settings** (verified via API):
```json
{
  "quality_llm": "openai/gpt-4.1",        // ❌ INVALID
  "fast_llm": "openai/gpt-4.1-mini",      // ❌ INVALID  
  "reasoning_llm": "openai/o3-mini"       // ✅ VALID
}
```

## Required Correct Configuration

**Updated Configuration** (with correct model names):
```json
{
  "quality_llm": "openai/o4-mini",        // ✅ CORRECT
  "fast_llm": "openai/o4-mini",           // ✅ CORRECT
  "reasoning_llm": "openai/o3-mini"       // ✅ KEEP AS IS
}
```

## Implementation Steps

### Step 1: Access R2R Server (192.168.1.4)

Since R2R is running on 192.168.1.4:7272, you need to access that server:

```bash
# SSH to R2R server
ssh user@192.168.1.4

# Or if running in Docker, access the container
docker exec -it r2r-container bash
```

### Step 2: Locate R2R Configuration File

Common configuration file locations:
```bash
# Check common locations
ls -la /etc/r2r/config.json
ls -la /opt/r2r/config.json
ls -la ./config.json
ls -la ./r2r.toml

# Search for configuration files
find / -name "*r2r*" -type f \( -name "*.json" -o -name "*.toml" -o -name "*.yaml" \) 2>/dev/null

# Check Docker volume mounts (if containerized)
docker inspect r2r-container | grep -A 10 "Mounts"
```

### Step 3: Backup Current Configuration

```bash
# Backup the configuration file
cp /path/to/r2r/config.json /path/to/r2r/config.json.backup.$(date +%Y%m%d_%H%M%S)
```

### Step 4: Update Configuration

**Option A: JSON Configuration File**
```bash
# Edit the configuration file
nano /path/to/r2r/config.json

# Update the model names:
# Change "openai/gpt-4.1" to "openai/o4-mini"
# Change "openai/gpt-4.1-mini" to "openai/o4-mini"
```

**Option B: TOML Configuration File**
```bash
# Edit the TOML file
nano /path/to/r2r/r2r.toml

# Update the [app] section:
[app]
quality_llm = "openai/o4-mini"
fast_llm = "openai/o4-mini"
reasoning_llm = "openai/o3-mini"
```

**Option C: Using sed for Quick Update**
```bash
# For JSON files
sed -i 's/"openai\/gpt-4\.1"/"openai\/o4-mini"/g' /path/to/r2r/config.json
sed -i 's/"openai\/gpt-4\.1-mini"/"openai\/o4-mini"/g' /path/to/r2r/config.json

# For TOML files  
sed -i 's/quality_llm = "openai\/gpt-4\.1"/quality_llm = "openai\/o4-mini"/g' /path/to/r2r/r2r.toml
sed -i 's/fast_llm = "openai\/gpt-4\.1-mini"/fast_llm = "openai\/o4-mini"/g' /path/to/r2r/r2r.toml
```

### Step 5: Restart R2R Service

**Docker Container:**
```bash
# Restart the container
docker restart r2r-container

# Or stop and start
docker stop r2r-container
docker start r2r-container
```

**System Service:**
```bash
# Restart system service
sudo systemctl restart r2r
sudo systemctl status r2r

# Check logs
sudo journalctl -u r2r -f
```

**Manual Process:**
```bash
# Kill existing process
pkill -f r2r

# Start R2R with new configuration
cd /path/to/r2r
./r2r serve --config config.json
```

## Verification Steps

### Step 1: Verify Configuration Update
```bash
# Check R2R system settings via API
curl -H "Authorization: super-secret-admin-key" \
  "http://192.168.1.4:7272/v3/system/settings" | \
  grep -E "(quality_llm|fast_llm|reasoning_llm)"
```

**Expected Output:**
```json
{
  "quality_llm": "openai/o4-mini",
  "fast_llm": "openai/o4-mini", 
  "reasoning_llm": "openai/o3-mini"
}
```

### Step 2: Test LLM Completion
```bash
# Test completion endpoint
curl -X POST -H "Authorization: super-secret-admin-key" \
  -H "Content-Type: application/json" \
  -d '{"messages":[{"role":"user","content":"Hello, test completion"}]}' \
  "http://192.168.1.4:7272/v3/retrieval/completion"
```

**Expected:** Valid JSON response with AI-generated content (not 500 error)

### Step 3: Test Conversation Message
```bash
# Send test message to conversation
curl -X POST -H "Authorization: super-secret-admin-key" \
  -H "Content-Type: application/json" \
  -d '{"content":"Test with correct model","role":"user","collection_ids":["122fdf6a-e116-546b-a8f6-e4cb2e2c0a09"]}' \
  "http://192.168.1.4:7272/v3/conversations/f805b4f3-47b3-44a4-af72-93bd648e0c0b/messages"
```

### Step 4: Verify Assistant Response Generated
```bash
# Check conversation for assistant response
curl -H "Authorization: super-secret-admin-key" \
  "http://192.168.1.4:7272/v3/conversations/f805b4f3-47b3-44a4-af72-93bd648e0c0b"
```

**Expected:** Conversation should now contain both user and assistant messages

### Step 5: Test WebUI Chat Interface
1. Navigate to WebUI: `http://localhost:5170/chat`
2. Select "Skin AI Collection"
3. Send test message: "What can you tell me about skin disease classification?"
4. **Expected:** AI-generated response appears (not fallback message)

## Troubleshooting

### Configuration File Not Found
```bash
# Check R2R process and working directory
ps aux | grep r2r
lsof -p <r2r_process_id> | grep -E "\.(json|toml)$"

# Check environment variables
env | grep -i r2r
```

### Permission Issues
```bash
# Check file permissions
ls -la /path/to/r2r/config.json

# Fix permissions if needed
sudo chown r2r:r2r /path/to/r2r/config.json
sudo chmod 644 /path/to/r2r/config.json
```

### Service Won't Start
```bash
# Check R2R logs
sudo journalctl -u r2r -n 50

# Check configuration syntax
python -m json.tool /path/to/r2r/config.json  # For JSON
# Or use TOML validator for TOML files
```

## Expected Outcomes

### Before Fix
- ❌ LLM completion returns 500 Internal Server Error
- ❌ Conversations show only user messages
- ❌ Chat interface displays fallback responses
- ❌ R2R logs show model validation errors

### After Fix  
- ✅ LLM completion returns valid AI responses
- ✅ Conversations contain both user and assistant messages
- ✅ Chat interface displays real AI responses based on 71 indexed documents
- ✅ R2R logs show successful model initialization

## Next Steps

1. **Apply the configuration fix** on the R2R server (192.168.1.4)
2. **Restart the R2R service** to load new configuration
3. **Run verification tests** to confirm the fix works
4. **Test end-to-end chat functionality** in WebUI
5. **Monitor system logs** for any remaining issues

---

**Critical Action Required**: Access R2R server at 192.168.1.4 and update configuration file with correct `openai/o4-mini` model names.

**Configuration Location**: R2R server at 192.168.1.4 (not accessible from current machine)  
**Fix Priority**: CRITICAL - Required for chat functionality  
**Estimated Time**: 5-10 minutes once server access is available
