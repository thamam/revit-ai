#!/bin/bash

# Claude Code Plan Mode Configuration Script
# This script helps configure read-only tool permissions for plan mode

set -e

# Color codes for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║   Claude Code Plan Mode Configuration Tool            ║${NC}"
echo -e "${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo ""

# Check if jq is installed
if ! command -v jq &> /dev/null; then
    echo -e "${RED}Error: 'jq' is required but not installed.${NC}"
    echo "Please install jq: sudo apt-get install jq (Ubuntu/Debian) or brew install jq (macOS)"
    exit 1
fi

# Define all available Claude Code tools
declare -A TOOLS_INFO
TOOLS_INFO=(
    # Read-only tools (safe for plan mode)
    ["Read"]="read-only|Read files from the filesystem (already allowed by default in plan mode)"
    ["Glob"]="read-only|Fast file pattern matching (already allowed by default in plan mode)"
    ["Grep"]="read-only|Search file contents using regex (already allowed by default in plan mode)"
    ["NotebookRead"]="read-only|Read Jupyter notebook files (already allowed by default in plan mode)"
    ["WebFetch"]="read-only|Fetch content from URLs to analyze"
    ["WebSearch"]="read-only|Search the web for information"
    ["Task"]="read-only|Launch specialized AI agents for complex tasks"
    ["BashOutput"]="read-only|Read output from background bash processes"
    ["AskUserQuestion"]="read-only|Ask the user questions during execution"

    # Write/Execute tools (NOT recommended for plan mode)
    ["Bash"]="write|Execute bash commands (can modify system)"
    ["Edit"]="write|Edit existing files (modifies files)"
    ["Write"]="write|Write new files or overwrite existing ones"
    ["NotebookEdit"]="write|Edit Jupyter notebook cells"
    ["KillShell"]="write|Kill background bash processes"
    ["SlashCommand"]="write|Execute custom slash commands (may modify system)"
    ["Skill"]="write|Execute skills that may modify the system"
)

# Function to display tools
display_tools() {
    echo -e "${BLUE}Available Claude Code Tools:${NC}"
    echo ""
    echo -e "${GREEN}═══ READ-ONLY TOOLS (Safe for Plan Mode) ═══${NC}"
    for tool in "${!TOOLS_INFO[@]}"; do
        IFS='|' read -r type desc <<< "${TOOLS_INFO[$tool]}"
        if [[ "$type" == "read-only" ]]; then
            echo -e "  ${GREEN}✓${NC} ${YELLOW}$tool${NC}"
            echo -e "    $desc"
        fi
    done

    echo ""
    echo -e "${RED}═══ WRITE/EXECUTE TOOLS (NOT Recommended) ═══${NC}"
    for tool in "${!TOOLS_INFO[@]}"; do
        IFS='|' read -r type desc <<< "${TOOLS_INFO[$tool]}"
        if [[ "$type" == "write" ]]; then
            echo -e "  ${RED}✗${NC} ${YELLOW}$tool${NC}"
            echo -e "    $desc"
        fi
    done
    echo ""
}

# Function to get settings file path
get_settings_path() {
    echo ""
    echo -e "${BLUE}Select configuration scope:${NC}"
    echo "  1) User-level (applies to all projects)"
    echo "     Location: ~/.claude/settings.json"
    echo ""
    echo "  2) Project-level (shared with team, checked into git)"
    echo "     Location: <project>/.claude/settings.json"
    echo ""
    echo "  3) Local project (personal, not checked into git)"
    echo "     Location: <project>/.claude/settings.local.json"
    echo ""
    read -p "Enter choice (1-3): " scope_choice

    case $scope_choice in
        1)
            settings_path="$HOME/.claude/settings.json"
            ;;
        2)
            read -p "Enter project path: " project_path
            if [[ ! -d "$project_path" ]]; then
                echo -e "${RED}Error: Directory does not exist${NC}"
                exit 1
            fi
            settings_path="$project_path/.claude/settings.json"
            mkdir -p "$project_path/.claude"
            ;;
        3)
            read -p "Enter project path: " project_path
            if [[ ! -d "$project_path" ]]; then
                echo -e "${RED}Error: Directory does not exist${NC}"
                exit 1
            fi
            settings_path="$project_path/.claude/settings.local.json"
            mkdir -p "$project_path/.claude"
            ;;
        *)
            echo -e "${RED}Invalid choice${NC}"
            exit 1
            ;;
    esac

    echo -e "${GREEN}Configuration file: $settings_path${NC}"
}

# Function to select tools
select_tools() {
    echo ""
    echo -e "${BLUE}Which read-only tools would you like to allow in plan mode?${NC}"
    echo "(Tools marked with * are already allowed by default)"
    echo ""

    selected_tools=()

    # Recommended read-only tools (excluding defaults)
    read_only_tools=("WebFetch" "WebSearch" "Task" "BashOutput" "AskUserQuestion")

    for tool in "${read_only_tools[@]}"; do
        read -p "Allow $tool? (y/n): " -n 1 -r
        echo ""
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            selected_tools+=("$tool")
        fi
    done

    # Ask about domain-specific WebFetch permissions
    if [[ " ${selected_tools[@]} " =~ " WebFetch " ]]; then
        echo ""
        echo -e "${YELLOW}WebFetch Domain Configuration${NC}"
        echo "You can restrict WebFetch to specific domains for security."
        read -p "Add domain-specific WebFetch permissions? (y/n): " -n 1 -r
        echo ""
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            while true; do
                read -p "Enter domain (e.g., github.com) or 'done' to finish: " domain
                if [[ "$domain" == "done" ]]; then
                    break
                fi
                selected_tools+=("WebFetch(domain:$domain)")
            done
        fi
    fi

    echo ""
    echo -e "${BLUE}Advanced: Add any other tools?${NC}"
    read -p "Show all tools including write/execute tools? (y/n): " -n 1 -r
    echo ""
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo ""
        display_tools
        echo ""
        read -p "Enter additional tools (comma-separated) or press Enter to skip: " additional
        if [[ -n "$additional" ]]; then
            IFS=',' read -ra ADDR <<< "$additional"
            for tool in "${ADDR[@]}"; do
                tool=$(echo "$tool" | xargs) # trim whitespace
                selected_tools+=("$tool")
            done
        fi
    fi
}

# Function to update settings file
update_settings() {
    local settings_path=$1
    shift
    local tools=("$@")

    # Create backup if file exists
    if [[ -f "$settings_path" ]]; then
        cp "$settings_path" "$settings_path.backup"
        echo -e "${GREEN}Created backup: $settings_path.backup${NC}"
    fi

    # Initialize settings if file doesn't exist
    if [[ ! -f "$settings_path" ]]; then
        echo '{}' > "$settings_path"
    fi

    # Read existing settings
    existing_settings=$(cat "$settings_path")

    # Create tools array for jq
    tools_json=$(printf '%s\n' "${tools[@]}" | jq -R . | jq -s .)

    # Update settings with jq
    updated_settings=$(echo "$existing_settings" | jq \
        --argjson tools "$tools_json" \
        '.defaultMode = "plan" |
         .permissions.allow = ($tools + (.permissions.allow // []) | unique) |
         .permissions.deny = (.permissions.deny // []) |
         .permissions.ask = (.permissions.ask // [])')

    # Write updated settings
    echo "$updated_settings" > "$settings_path"

    echo -e "${GREEN}✓ Settings updated successfully!${NC}"
    echo ""
    echo -e "${BLUE}Updated configuration:${NC}"
    cat "$settings_path" | jq .
}

# Main execution
main() {
    display_tools
    get_settings_path
    select_tools

    echo ""
    echo -e "${BLUE}Summary of selected tools:${NC}"
    if [[ ${#selected_tools[@]} -eq 0 ]]; then
        echo -e "${YELLOW}No additional tools selected (only default read-only tools will be available)${NC}"
    else
        for tool in "${selected_tools[@]}"; do
            echo "  - $tool"
        done
    fi

    echo ""
    read -p "Proceed with configuration? (y/n): " -n 1 -r
    echo ""
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        update_settings "$settings_path" "${selected_tools[@]}"
        echo ""
        echo -e "${GREEN}╔════════════════════════════════════════════════════════╗${NC}"
        echo -e "${GREEN}║   Configuration Complete!                              ║${NC}"
        echo -e "${GREEN}╚════════════════════════════════════════════════════════╝${NC}"
        echo ""
        echo "To activate plan mode in your Claude Code session:"
        echo "  1. Press Shift+Tab to cycle through modes"
        echo "  2. Or restart Claude Code (it will use plan mode by default)"
    else
        echo -e "${YELLOW}Configuration cancelled${NC}"
    fi
}

main
