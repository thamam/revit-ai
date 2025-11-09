#!/usr/bin/env python3
"""
Test Claude API connection before building the Revit plugin.
This validates your API key and Claude's ability to analyze schedule data.

Usage:
1. Set your API key below
2. Run: python test_claude_api.py
"""

import json
import requests

# ============= CONFIGURATION =============
ANTHROPIC_API_KEY = "YOUR_API_KEY_HERE"  # Replace with your key
CLAUDE_MODEL = "claude-sonnet-4-20250514"
API_URL = "https://api.anthropic.com/v1/messages"
# =========================================

# Sample schedule data (simulates what Revit would send)
SAMPLE_SCHEDULE = {
    "Name": "Wall Schedule",
    "Category": "Walls",
    "Headers": ["Mark", "Type", "Length", "Height", "Area", "Comments"],
    "Rows": [
        ["W-01", "Interior - 6\"", "10'-0\"", "9'-0\"", "90 SF", "Conference Room"],
        ["W-02", "Interior - 6\"", "12'-0\"", "9'-0\"", "108 SF", ""],
        ["W-03", "Interior - 6\"", "10'-0\"", "9'-0\"", "90 SF", "Break Room"],
        ["W-04", "Exterior - 8\"", "20'-0\"", "12'-0\"", "240 SF", "North Wall"],
        ["W-05", "Interior - 6\"", "8'-0\"", "9'-0\"", "72 SF", ""],
        ["W-06", "Interior - 6\"", "10'-0\"", "9'-0\"", "90 SF", "Storage"],
    ],
    "RowCount": 6,
    "ColumnCount": 6
}

PROMPT_TEMPLATE = """You are an expert in architectural documentation and Revit scheduling.

I have a schedule from a Revit model with the following data:

Schedule Name: {name}
Category: {category}
Number of Rows: {row_count}
Number of Columns: {col_count}

Raw Data (JSON):
```json
{data_json}
```

Please analyze this schedule and provide:

1. **Summary**: Brief overview of what this schedule represents
2. **Data Quality Assessment**: Any missing data, inconsistencies, or issues you notice
3. **Professional Formatting Suggestions**: How to improve this schedule for construction documentation
4. **Insights**: Any patterns, outliers, or notable findings in the data

Format your response in clear sections with headers. Be specific and actionable."""


def test_claude_api():
    """Test Claude API connection and schedule analysis"""
    
    print("=" * 60)
    print("TESTING CLAUDE API FOR REVIT SCHEDULE ANALYSIS")
    print("=" * 60)
    print()
    
    # Validate API key
    if ANTHROPIC_API_KEY == "YOUR_API_KEY_HERE":
        print("❌ ERROR: Please set your Anthropic API key in the script")
        print("   Get your key from: https://console.anthropic.com/")
        return False
    
    print("✓ API Key configured")
    print(f"✓ Model: {CLAUDE_MODEL}")
    print()
    
    # Build the prompt
    prompt = PROMPT_TEMPLATE.format(
        name=SAMPLE_SCHEDULE["Name"],
        category=SAMPLE_SCHEDULE["Category"],
        row_count=SAMPLE_SCHEDULE["RowCount"],
        col_count=SAMPLE_SCHEDULE["ColumnCount"],
        data_json=json.dumps(SAMPLE_SCHEDULE, indent=2)
    )
    
    print("Sending sample wall schedule to Claude...")
    print()
    
    # Prepare request
    headers = {
        "x-api-key": ANTHROPIC_API_KEY,
        "anthropic-version": "2023-06-01",
        "content-type": "application/json"
    }
    
    payload = {
        "model": CLAUDE_MODEL,
        "max_tokens": 2000,
        "messages": [
            {
                "role": "user",
                "content": prompt
            }
        ]
    }
    
    # Send request
    try:
        response = requests.post(API_URL, headers=headers, json=payload, timeout=30)
        
        if response.status_code != 200:
            print(f"❌ ERROR: API returned status {response.status_code}")
            print(f"   Response: {response.text}")
            return False
        
        # Parse response
        data = response.json()
        claude_response = data["content"][0]["text"]
        
        print("✓ Successfully received response from Claude!")
        print()
        print("=" * 60)
        print("CLAUDE'S ANALYSIS:")
        print("=" * 60)
        print()
        print(claude_response)
        print()
        print("=" * 60)
        print()
        print("✅ TEST PASSED: Claude API is working correctly")
        print()
        print("Next steps:")
        print("1. Copy your API key to ClaudeAPIClient.cs")
        print("2. Build the Revit plugin in Visual Studio")
        print("3. Test with real Revit project data")
        print()
        
        return True
        
    except requests.exceptions.Timeout:
        print("❌ ERROR: Request timed out")
        print("   Check your internet connection")
        return False
        
    except requests.exceptions.RequestException as e:
        print(f"❌ ERROR: Network error: {e}")
        return False
        
    except Exception as e:
        print(f"❌ ERROR: {type(e).__name__}: {e}")
        return False


if __name__ == "__main__":
    success = test_claude_api()
    exit(0 if success else 1)
