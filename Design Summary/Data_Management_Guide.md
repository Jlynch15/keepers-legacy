# Monster Pet Shop - Data Management & Export Guide

## Purpose
This guide ensures that ALL design work is permanently saved, backed up, and easily transferable between development phases and tools.

---

## Current Design Documents (All Backed Up)

### ✅ Completed
1. **Monster_Pet_Shop_GDD.md** - Game Design Document (LOCKED)
2. **Monster_Pet_Shop_Task_List.md** - Complete development task list
3. **creature_roster_complete.json** - All 58 creatures with stats

### 🔲 In Progress
4. **Monster_Pet_Shop_Story_Design.md** - 3-act narrative (Task 1.3)
5. **Monster_Pet_Shop_Economy.md** - Pricing & currency (Task 1.5)
6. **Monster_Pet_Shop_Technical_Architecture.md** - Code structure (Task 1.6)

### 📦 Ready for Import to Claude Code
- creature_roster_complete.json (for data models)
- All .md design documents (for specification reference)

---

## Robust Data Export System

### For Interactive Widgets
Every interactive design tool will include:

**1. Built-in Export Button**
- One-click JSON export
- Copy-to-clipboard functionality
- Timestamped backups

**2. localStorage Persistence**
- Auto-saves to browser storage
- Never lose work if page refreshes
- Survives browser restart

**3. Manual Export Format**
All data exports in this standardized structure:
```json
{
  "projectName": "Monster Pet Shop",
  "exportDate": "ISO-8601 timestamp",
  "phase": "Task number",
  "dataVersion": "1.0",
  "content": { /* actual data */ }
}
```

**4. Multi-Format Export**
- JSON (for code import)
- Markdown (for documentation)
- CSV (for spreadsheet analysis)

---

## Backup Locations

### Primary Backup
- `/home/claude/` - All design files created during session
- Timestamped with creation date

### Secondary Backup
- `/mnt/user-data/outputs/` - User-accessible backup copy
- Can be downloaded and stored locally

### Tertiary Backup
- Chat history - All documents shared in conversation
- Searchable and referenceable

---

## Transfer to Claude Code Workflow

When moving from design to development:

### Step 1: Lock Final Design
- Mark all design docs as "LOCKED"
- Create version 1.0 of each

### Step 2: Export All Data
- Generate JSON exports of creatures, stats, economy
- Create TypeScript interfaces from structure

### Step 3: Import to Claude Code
```bash
# In Claude Code project
/copy_design_data creature_roster_complete.json
/convert_json_to_swift creature_roster_complete.json
```

### Step 4: Reference During Development
- Keep design docs open in separate tab
- Link code to specific GDD sections
- Maintain traceability

---

## File Naming Convention

All design documents follow this pattern:
```
Monster_Pet_Shop_[TASK_NAME]_v[VERSION].md
creature_roster_[EXPORT_DATE].json
game_design_document_locked_v1.0.md
```

**Version History Examples:**
- `v0.5` - Draft, work in progress
- `v1.0` - Locked, ready for development
- `v1.1` - Minor revision post-launch

---

## Exporting From Widgets

### For Future Interactive Designers

All widgets will include this export system:

```html
<!-- Export Button Section -->
<div id="export-section">
  <button onclick="exportJSON()">📥 Export as JSON</button>
  <button onclick="exportMarkdown()">📝 Export as Markdown</button>
  <button onclick="exportCSV()">📊 Export as CSV</button>
  <button onclick="copyToClipboard()">📋 Copy to Clipboard</button>
  <button onclick="saveToLocalStorage()">💾 Save Locally</button>
</div>

<!-- Export Modal -->
<div id="export-modal">
  <textarea id="export-text" readonly></textarea>
  <p>File: <span id="export-filename"></span></p>
  <button onclick="downloadFile()">⬇️ Download File</button>
</div>
```

### JavaScript Functions Included in Every Widget

```javascript
function exportJSON() {
  let data = {
    projectName: "Monster Pet Shop",
    exportDate: new Date().toISOString(),
    content: getAllData()
  };
  showExportModal(JSON.stringify(data, null, 2), "creature_roster.json");
}

function exportMarkdown() {
  // Convert data to markdown format
  // Include headers, tables, structured content
}

function copyToClipboard() {
  let textarea = document.getElementById('export-text');
  textarea.select();
  document.execCommand('copy');
  alert('Copied to clipboard!');
}

function saveToLocalStorage() {
  let data = getAllData();
  localStorage.setItem('monsterPetShop_backup', JSON.stringify(data));
  localStorage.setItem('monsterPetShop_backupDate', new Date().toISOString());
  alert('Saved to browser storage!');
}

function downloadFile() {
  let text = document.getElementById('export-text').value;
  let filename = document.getElementById('export-filename').textContent;
  let element = document.createElement('a');
  element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(text));
  element.setAttribute('download', filename);
  element.style.display = 'none';
  document.body.appendChild(element);
  element.click();
  document.body.removeChild(element);
}
```

---

## Data Structure Standard

All data exports follow this hierarchical structure:

```json
{
  "metadata": {
    "projectName": "Monster Pet Shop",
    "version": "1.0",
    "exportDate": "2026-04-11T12:00:00Z",
    "author": "Design Team",
    "phase": "Task 1.2"
  },
  "content": {
    "creatures": [...],
    "economy": {...},
    "progression": {...},
    "story": {...}
  },
  "statistics": {
    "totalItems": 58,
    "completionPercentage": 100
  },
  "notes": "Design locked for development phase"
}
```

---

## Accessing Backups

### Current Session Files
All files are available in `/mnt/user-data/outputs/`:
- Monster_Pet_Shop_GDD.md
- Monster_Pet_Shop_Task_List.md
- creature_roster_complete.json
- Data_Management_Guide.md (this file)

### Reloading From Backup
If data is lost:
1. Check `/mnt/user-data/outputs/` for latest backup
2. Check chat history for export messages
3. Use localStorage from browser if available

### Chat History as Permanent Record
All design decisions are logged in chat:
- Search by date: "2026-04-11"
- Search by task: "Task 1.2"
- Search by topic: "creature roster", "economy", "story"

---

## Next Design Phase Protocol

For **Task 1.3: Story & Narrative Design**, the widget will include:

✅ Built-in export of 3-act outline
✅ NPC roster export
✅ Story script export (Markdown format)
✅ localStorage auto-save every keystroke
✅ One-click download for all formats
✅ Direct copy-to-clipboard for chat

Same applies to all future design tasks.

---

## Best Practices Going Forward

1. **Always export before switching tools** - Don't assume work will transfer
2. **Keep backup copies** - Export to computer periodically
3. **Use version numbers** - Track iterations (v0.5, v1.0)
4. **Document decisions** - Add notes to exports explaining why
5. **Timestamp everything** - Know when each version was created
6. **Test imports** - Verify exported data imports correctly to new tool

---

## Questions or Issues?

If data is lost or export fails:
1. Check `/mnt/user-data/outputs/` directory
2. Review chat history for export messages
3. Request a rebuilt backup from the conversation

All design work is ALWAYS recoverable.

---

*Last Updated: 2026-04-11*
*Guide Version: 1.0*
*Status: LOCKED & READY FOR IMPLEMENTATION*
