# Epic 2: Intelligent Automation (Refactored - Research-Informed)

**Date:** 2025-11-20
**Status:** Planned
**Research Basis:** Tasks 1, 2, 3 findings from MCP Server Landscape, PyRevit+LLM Integration, and Testing Strategies research

---

## Epic Overview

**Goal:** Enable architects to automate tedious Revit tasks through natural language commands, prioritizing high-value, low-risk operations that demonstrate the complete User→LLM→MCP→Revit flow.

**Strategic Pivot Rationale:**
Research findings (Task 2: PyRevit+LLM Integration Analysis) identified that commercial products (ArchiLabs, BIMLOGIQ) and user interviews (Studio Tema case study) prioritize **annotation tasks** (tagging, parameters) over **geometric tasks** (dimensions, walls). Auto-tagging emerged as the #1 pain point: "Users spend days manually tagging wall types and door numbers."

**Risk Mitigation:** Task 3 (Testing Strategies Research) emphasizes "Read-Only/Annotation" tasks have lower blast radius than geometric modifications. Studio Tema case study shows firms quantify automation risk at 20,000 ILS and mandate sandbox testing. Starting with tagging reduces risk while building user trust.

**Epic Structure:**
- **Stories 2.1-2.3:** Auto-Tagging Implementation (High Value, Low Risk) - NEW PRIORITY
- **Stories 2.4-2.6:** Dimension Automation (Medium Value, Medium Risk) - DEFERRED FROM ORIGINAL
- **Story 2.7:** MCP Compatibility Layer - STRATEGIC ENABLER

---

## Story 2.1: Auto-Tagging Command Parser & Safety Validation

**As an architect,**
I want to auto-tag elements using natural language commands,
So that I can save days of manual tagging work while ensuring safe, preview-confirmed operations.

### Acceptance Criteria

**Given** a Hebrew or English tagging prompt (e.g., "תייג את כל הדלתות בקומה 1", "Tag all walls in current view")
**When** the prompt is sent to Claude API with Revit context
**Then** a structured action is returned with:
- Operation type: `auto_tag`
- Target elements: category (Walls/Doors/Rooms), scope (current view, level, selection)
- Tag parameters: tag type, placement strategy (leader/no leader), offset

**And** ambiguous prompts trigger clarifying questions:
- "Which tag type? [Door Tag | Door Number | Custom]"
- "Tag all doors or only untagged doors?"

**And** safety validation enforces:
- Maximum 500 elements per operation (configurable)
- Only "read + annotate" operations allowed (no geometry modification)
- Tags are metadata additions (reversible with Ctrl+Z)

**And** Hebrew and English prompts handled equally

### Prerequisites
- Story 1.1: Project scaffold, ribbon UI
- Story 1.2: Claude API integration
- Story 1.4: Safety validation framework
- Story 1.7: Configuration system

### Technical Implementation

**Claude System Prompt Enhancement:**
```csharp
string systemPrompt = @"
You are RevitAI, an AI assistant for Autodesk Revit automation.

AVAILABLE OPERATIONS:
- auto_tag: Add tags to elements (walls, doors, windows, rooms, etc.)
- read_elements: Query element properties
- (dimension operations available in later release)

CONTEXT:
<current_view>Level 1 Floor Plan</current_view>
<available_levels>Level 1, Level 2, Roof</available_levels>
<element_summary>
  Doors: 24 (12 untagged)
  Walls: 156 (87 untagged)
  Rooms: 18 (0 untagged)
</element_summary>
<available_tag_types>
  Door Tag, Door Number, Wall Tag, Room Tag, Room Number
</available_tag_types>

USER PROMPT: {user_prompt}

Return JSON:
{
  ""operation"": ""auto_tag"",
  ""targets"": {
    ""category"": ""Doors"",
    ""scope"": ""current_view"",
    ""filter"": ""untagged_only""
  },
  ""parameters"": {
    ""tag_type"": ""Door Tag"",
    ""placement"": ""center"",
    ""leader"": false
  },
  ""clarifications"": []
}
";
```

**Safety Validator Extension:**
```csharp
// Services/SafetyValidator.cs
public ValidationResult ValidateAutoTagOperation(RevitAction action) {
    // Check operation allowlist
    if (action.Operation != "auto_tag") {
        return ValidationResult.Rejected("Only auto_tag operation allowed in this release");
    }

    // Check scope limits
    int elementCount = GetElementCount(action.Targets);
    if (elementCount > MAX_ELEMENTS_PER_OPERATION) {
        return ValidationResult.Rejected(
            $"Too many elements ({elementCount}). Maximum: {MAX_ELEMENTS_PER_OPERATION}"
        );
    }

    // Validate tag type exists
    var tagType = GetTagType(action.Parameters["tag_type"]);
    if (tagType == null) {
        return ValidationResult.Rejected(
            $"Tag type '{action.Parameters["tag_type"]}' not found in project"
        );
    }

    return ValidationResult.Approved();
}
```

### Testing Strategy

**Unit Tests** (Layer 1 - Decoupled Logic):
```csharp
[TestClass]
public class AutoTagParserTests {
    [TestMethod]
    public async Task ParseHebrewTagPrompt_ReturnsAutoTagAction() {
        var parser = new ClaudeService(mockApiKey);
        var prompt = "תייג את כל הדלתות בקומה 1";

        var action = await parser.ParsePromptAsync(prompt, context);

        Assert.AreEqual("auto_tag", action.Operation);
        Assert.AreEqual("Doors", action.Targets["category"]);
        Assert.AreEqual("Level 1", action.Targets["scope"]);
    }

    [TestMethod]
    public void ValidateAutoTagScope_ExceedsLimit_ReturnsRejected() {
        var validator = new SafetyValidator();
        var action = new RevitAction {
            Operation = "auto_tag",
            Targets = { ["element_count"] = 600 } // Exceeds 500 limit
        };

        var result = validator.ValidateAutoTagOperation(action);

        Assert.IsFalse(result.IsApproved);
        Assert.IsTrue(result.ErrorMessage.Contains("Too many elements"));
    }
}
```

**Integration Tests** (Layer 2 - Mocked Revit API):
```csharp
[TestMethod]
public async Task AutoTagWorkflow_EndToEnd_CreatesTagsSuccessfully() {
    // Arrange: Mock Revit document with 12 untagged doors
    var mockDoc = MockRevitDocument.CreateWithElements(
        doors: 12, untagged: 12
    );

    // Act: Execute auto-tag command
    var result = await _commandHandler.ExecuteAsync(
        prompt: "Tag all doors",
        document: mockDoc
    );

    // Assert: 12 tags created
    Assert.AreEqual(12, mockDoc.GetCreatedTags().Count);
    Assert.AreEqual("Success", result.Status);
}
```

### Definition of Done
- [ ] Claude parses Hebrew and English tagging prompts accurately (>90% success rate on test set)
- [ ] Safety validator enforces scope limits and operation allowlist
- [ ] Unit tests pass (parsing + validation logic)
- [ ] Integration tests pass with mocked Revit API
- [ ] Code reviewed and merged to main branch
- [ ] Documentation updated (architecture.md, CLAUDE.md)

---

## Story 2.2: Tag Placement Engine with Spatial Intelligence

**As a developer,**
I want to implement intelligent tag placement logic,
So that tags don't overlap and follow professional standards.

### Acceptance Criteria

**Given** a list of target elements (e.g., 24 doors) from parsed scope
**When** tag placement is calculated
**Then** each tag is positioned:
- At element center (default) or user-specified offset
- With collision avoidance (no overlapping tags)
- With appropriate leader line (if element is small or obscured)
- Respecting firm standards (offset distance, orientation)

**And** placement algorithm handles:
- Elements in different views (plan, elevation, section)
- Rotated elements (angled walls, diagonal doors)
- Elements with existing tags (skip or replace based on user preference)

**And** placement failures are logged:
- "Could not place tag for Door ID 12345: No valid placement found"
- Partial success supported: "Tagged 22 of 24 doors (2 failed)"

### Prerequisites
- Story 2.1: Auto-tagging parser and validation
- Story 1.3: ExternalEvent pattern (thread-safe Revit API access)

### Technical Implementation

**Tag Placement Service:**
```csharp
// Services/TagPlacementService.cs
public class TagPlacementService {
    private readonly ITagCollisionDetector _collisionDetector;

    public List<TagPlacement> CalculatePlacements(
        IEnumerable<Element> elements,
        TagType tagType,
        PlacementStrategy strategy
    ) {
        var placements = new List<TagPlacement>();
        var existingTags = GetExistingTagBounds(); // For collision detection

        foreach (var element in elements) {
            var candidate = strategy.GetPreferredPlacement(element);

            // Collision avoidance
            int attempts = 0;
            while (_collisionDetector.HasCollision(candidate, existingTags) && attempts < 10) {
                candidate = strategy.GetAlternativePlacement(element, attempts);
                attempts++;
            }

            if (attempts >= 10) {
                placements.Add(TagPlacement.Failed(element, "No collision-free placement found"));
            } else {
                placements.Add(TagPlacement.Success(element, candidate));
                existingTags.Add(candidate.Bounds); // Track for next iteration
            }
        }

        return placements;
    }
}

public interface IPlacementStrategy {
    TagPlacementCandidate GetPreferredPlacement(Element element);
    TagPlacementCandidate GetAlternativePlacement(Element element, int attempt);
}

// Default strategy: Center with offset
public class CenterOffsetStrategy : IPlacementStrategy {
    private readonly double _offsetDistance;

    public TagPlacementCandidate GetPreferredPlacement(Element element) {
        var bbox = element.get_BoundingBox(view);
        var center = (bbox.Min + bbox.Max) / 2;

        // Offset upward by configured distance
        var tagPoint = center + new XYZ(0, _offsetDistance, 0);

        return new TagPlacementCandidate {
            Element = element,
            Location = tagPoint,
            Leader = false
        };
    }

    public TagPlacementCandidate GetAlternativePlacement(Element element, int attempt) {
        // Try different offsets: right, left, up-right, up-left, etc.
        var angles = new[] { 0, 90, 45, 135, 180, 225, 270, 315 };
        var angle = angles[attempt % angles.Length];
        var offset = _offsetDistance * (1 + attempt / 8.0); // Increase distance if needed

        var bbox = element.get_BoundingBox(view);
        var center = (bbox.Min + bbox.Max) / 2;
        var tagPoint = center + PolarOffset(angle, offset);

        return new TagPlacementCandidate {
            Element = element,
            Location = tagPoint,
            Leader = true // Use leader if placed far from center
        };
    }
}
```

**Collision Detection:**
```csharp
public interface ITagCollisionDetector {
    bool HasCollision(TagPlacementCandidate candidate, List<BoundingBoxXYZ> existingTags);
}

public class SimpleBoundingBoxCollisionDetector : ITagCollisionDetector {
    private const double BUFFER_MARGIN = 0.1; // feet

    public bool HasCollision(TagPlacementCandidate candidate, List<BoundingBoxXYZ> existingTags) {
        var candidateBounds = EstimateTagBounds(candidate);

        foreach (var existing in existingTags) {
            if (BoundsOverlap(candidateBounds, existing, BUFFER_MARGIN)) {
                return true;
            }
        }

        return false;
    }

    private BoundingBoxXYZ EstimateTagBounds(TagPlacementCandidate candidate) {
        // Estimate tag size based on text content
        // Typical tag: 0.3' wide x 0.15' tall
        var width = 0.3;
        var height = 0.15;

        return new BoundingBoxXYZ {
            Min = candidate.Location - new XYZ(width / 2, height / 2, 0),
            Max = candidate.Location + new XYZ(width / 2, height / 2, 0)
        };
    }
}
```

### Testing Strategy

**Unit Tests** (Pure Logic):
```csharp
[TestMethod]
public void CalculatePlacements_NoDoors_ReturnsEmptyList() {
    var service = new TagPlacementService(new SimpleBoundingBoxCollisionDetector());
    var placements = service.CalculatePlacements(
        elements: Enumerable.Empty<Element>(),
        tagType: mockTagType,
        strategy: new CenterOffsetStrategy(0.5)
    );

    Assert.AreEqual(0, placements.Count);
}

[TestMethod]
public void GetAlternativePlacement_MultipleAttempts_IncreasesDistance() {
    var strategy = new CenterOffsetStrategy(0.5);
    var mockDoor = MockElement.CreateDoor(location: XYZ.Zero);

    var placement1 = strategy.GetAlternativePlacement(mockDoor, attempt: 0);
    var placement2 = strategy.GetAlternativePlacement(mockDoor, attempt: 8);

    var distance1 = placement1.Location.DistanceTo(XYZ.Zero);
    var distance2 = placement2.Location.DistanceTo(XYZ.Zero);

    Assert.IsTrue(distance2 > distance1, "Later attempts should try farther placements");
}
```

### Definition of Done
- [ ] Tag placement algorithm handles 95%+ of elements without collision
- [ ] Spatial intelligence tries multiple placements before failing
- [ ] Unit tests verify collision detection and placement strategies
- [ ] Integration tests confirm tags don't overlap in realistic scenarios
- [ ] Performance: Calculate placements for 500 elements in <5 seconds

---

## Story 2.3: Auto-Tagging Execution with Preview & Audit Trail

**As an architect,**
I want to preview all proposed tags before creation,
So that I can verify placement and cancel if needed.

### Acceptance Criteria

**Given** tag placements have been calculated
**When** the preview dialog is shown
**Then** I see:
- Summary: "Preview: 24 Door Tags will be added"
- List of target elements (collapsible, with IDs)
- Visual preview in Revit view (temporary graphics showing tag locations)
- Status for each tag: ✓ Success | ⚠ Warning (collision) | ✗ Failed

**And** I can:
- Confirm to create all tags in a single Transaction
- Cancel to abort with no changes
- See details of failed placements

**And** after confirmation:
- All tags created atomically (transaction commits or rolls back)
- Success message: "Created 24 tags successfully"
- Audit log entry created with timestamp, user, operation details
- Operation is undoable with Ctrl+Z

**And** if errors occur during creation:
- Transaction rolls back
- Error message shown: "Tag creation failed: [reason]"
- Logs capture full error context for debugging

### Prerequisites
- Story 2.2: Tag placement engine
- Story 1.5: Preview/Confirm dialog pattern
- Story 1.6: Logging infrastructure

### Technical Implementation

**Preview Dialog Enhancement:**
```csharp
// UI/TagPreviewDialog.xaml.cs
public class TagPreviewDialog : PreviewConfirmDialog {
    public void ShowTagPreview(OperationPreview preview) {
        // Base dialog shows summary
        SummaryTextBlock.Text = preview.Description; // "24 Door Tags will be added"

        // Custom: Show detailed status
        var statusList = new List<TagPreviewItem>();
        foreach (var placement in preview.TagPlacements) {
            statusList.Add(new TagPreviewItem {
                ElementId = placement.Element.Id.IntegerValue,
                ElementName = $"Door {placement.Element.Name}",
                Status = placement.IsSuccess ? "Success" : "Failed",
                StatusIcon = placement.IsSuccess ? "✓" : "✗",
                FailureReason = placement.FailureReason
            });
        }

        TagStatusListView.ItemsSource = statusList;

        // Draw temporary graphics in Revit view
        DrawTemporaryTagGraphics(preview.TagPlacements);
    }

    private void DrawTemporaryTagGraphics(List<TagPlacement> placements) {
        // Use DirectContext3D for hardware-accelerated preview
        var previewGraphics = new List<GeometryObject>();

        foreach (var placement in placements.Where(p => p.IsSuccess)) {
            // Draw small rectangle at tag location
            var rect = CreateTagRectangle(placement.Location);
            previewGraphics.Add(rect);

            // Draw leader line if applicable
            if (placement.Leader) {
                var line = Line.CreateBound(placement.Element.Location.Point, placement.Location);
                previewGraphics.Add(line);
            }
        }

        _previewGraphicsId = _directContext.RegisterGraphics(previewGraphics);
    }
}
```

**Tag Creation Execution:**
```csharp
// Services/TagCreationService.cs
public class TagCreationService {
    private readonly LoggingService _logger;

    public async Task<OperationResult> CreateTagsAsync(
        Document doc,
        List<TagPlacement> placements,
        TagType tagType
    ) {
        var createdTags = new List<IndependentTag>();
        var failedPlacements = new List<TagPlacement>();

        using (var trans = new Transaction(doc, "AI: Auto-Tag Elements")) {
            trans.Start();

            try {
                foreach (var placement in placements.Where(p => p.IsSuccess)) {
                    try {
                        var reference = new Reference(placement.Element);
                        var tag = IndependentTag.Create(
                            doc,
                            tagType.Id,
                            doc.ActiveView.Id,
                            reference,
                            placement.Leader,
                            TagOrientation.Horizontal,
                            placement.Location
                        );

                        createdTags.Add(tag);
                        _logger.Debug($"Created tag for element {placement.Element.Id}");
                    } catch (Exception ex) {
                        _logger.Warning($"Failed to create tag for element {placement.Element.Id}: {ex.Message}");
                        failedPlacements.Add(placement);
                    }
                }

                trans.Commit();

                // Audit trail
                _logger.LogOperation("auto_tag", "SUCCESS",
                    $"Created {createdTags.Count} tags ({failedPlacements.Count} failed)");

                return OperationResult.Success(
                    $"Created {createdTags.Count} tags successfully",
                    createdTags.Count,
                    failedPlacements.Count
                );
            } catch (Exception ex) {
                trans.RollBack();
                _logger.Error("Tag creation transaction failed", "AUTO_TAG", ex);
                return OperationResult.Failure($"Transaction failed: {ex.Message}");
            }
        }
    }
}
```

**Audit Log Format:**
```
[2025-11-20 14:35:22] [INFO] [AUTO_TAG] Operation started: Tag 24 doors in Level 1 Floor Plan
[2025-11-20 14:35:23] [DEBUG] [AUTO_TAG] Calculated 24 placements (22 success, 2 failed)
[2025-11-20 14:35:25] [INFO] [AUTO_TAG] User confirmed operation
[2025-11-20 14:35:27] [SUCCESS] [AUTO_TAG] Created 22 tags (2 failed placement)
[2025-11-20 14:35:27] [WARNING] [AUTO_TAG] Failed to place tag for Door ID 12345: No collision-free placement found
[2025-11-20 14:35:27] [WARNING] [AUTO_TAG] Failed to place tag for Door ID 12346: Element outside view bounds
```

### Testing Strategy

**Integration Tests:**
```csharp
[TestMethod]
public async Task CreateTags_AllSuccess_CommitsTransaction() {
    var mockDoc = MockRevitDocument.CreateWithElements(doors: 10);
    var placements = CreateMockPlacements(mockDoc.Doors, allSuccess: true);
    var service = new TagCreationService(mockLogger);

    var result = await service.CreateTagsAsync(mockDoc, placements, mockTagType);

    Assert.IsTrue(result.IsSuccess);
    Assert.AreEqual(10, result.CreatedCount);
    Assert.AreEqual(0, result.FailedCount);
    Assert.IsTrue(mockDoc.TransactionCommitted);
}

[TestMethod]
public async Task CreateTags_SomeFailures_PartiallSuccess() {
    var mockDoc = MockRevitDocument.CreateWithElements(doors: 10);
    var placements = CreateMockPlacements(mockDoc.Doors, successCount: 8, failCount: 2);
    var service = new TagCreationService(mockLogger);

    var result = await service.CreateTagsAsync(mockDoc, placements, mockTagType);

    Assert.IsTrue(result.IsSuccess); // Partial success still counts as success
    Assert.AreEqual(8, result.CreatedCount);
    Assert.AreEqual(2, result.FailedCount);
    Assert.IsTrue(mockLogger.HasWarnings);
}
```

### Definition of Done
- [ ] Preview dialog shows detailed status for each tag placement
- [ ] Temporary graphics render in Revit view during preview
- [ ] Transaction commits atomically (all or nothing for batch)
- [ ] Partial success handled gracefully (some tags created, some skipped)
- [ ] Audit log captures operation details for compliance
- [ ] Undo (Ctrl+Z) works correctly after tag creation
- [ ] Error handling prevents crashes, rolls back on failure

---

## Stories 2.4-2.6: Dimension Automation (Deferred)

**Note:** Original Epic 2 stories for dimension automation (room boundary detection, dimension chain generation, preview workflow) are deferred to Epic 3 or later based on research findings showing auto-tagging has higher user priority and lower implementation risk.

**Rationale:** Task 1 MCP Server Landscape Analysis notes dimension placement is "high demand, technically difficult." Task 2 findings show all commercial products prioritize tagging over dimensions. Will implement dimensions after validating auto-tagging workflow and gathering user feedback.

**Placeholder Stories:**
- 2.4: Dimension Command Parser (deferred)
- 2.5: Room Boundary Detection & Dimension Chain Generation (deferred)
- 2.6: Dimension Preview & Hybrid LLM+Solver Approach (deferred)

See `docs/epic3-dimensions.md` for full dimension automation epic (to be created).

---

## Story 2.7: MCP Compatibility Layer (Strategic Enabler)

**As a developer,**
I want to expose RevitAI operations via Model Context Protocol (MCP),
So that any MCP-compatible host can interact with Revit through our tool layer.

### Acceptance Criteria

**Given** RevitAI services are implemented (ClaudeService, SafetyValidator, TagCreationService)
**When** an MCP host sends a JSON-RPC request
**Then** the request is:
- Validated against MCP protocol schema
- Routed to appropriate internal service
- Executed via ExternalEvent (thread-safe)
- Response returned as JSON-RPC reply

**And** MCP tool registry exposes:
- `get_revit_model_info`: Returns project metadata
- `list_levels`: Returns all levels and elevations
- `get_elements`: Query elements by category
- `auto_tag_elements`: Create tags (new in this epic)

**And** MCP server listens on:
- HTTP endpoint: `http://localhost:48884/mcp`
- JSON-RPC 2.0 format for requests/responses

**And** error handling returns proper JSON-RPC error codes:
- `-32600`: Invalid Request
- `-32601`: Method Not Found
- `-32602`: Invalid Params
- `-32603`: Internal Error

### Prerequisites
- Epic 1: All infrastructure (ClaudeService, ExternalEvent, Logging)
- Story 2.3: Auto-tagging execution complete
- ADR-009: MCP Compatibility Layer decision

### Technical Implementation

**MCP Server Infrastructure:**
```csharp
// Services/MCPServer.cs
public class MCPServer {
    private readonly ClaudeService _claudeService;
    private readonly RevitEventHandler _eventHandler;
    private readonly SafetyValidator _validator;
    private readonly TagCreationService _tagService;
    private HttpListener _listener;

    public void Start(int port = 48884) {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/mcp/");
        _listener.Start();

        Task.Run(() => ListenForRequests());
        _logger.Info($"MCP Server started on port {port}");
    }

    private async Task ListenForRequests() {
        while (_listener.IsListening) {
            var context = await _listener.GetContextAsync();
            _ = Task.Run(() => HandleRequest(context));
        }
    }

    private async Task HandleRequest(HttpListenerContext context) {
        try {
            // Parse JSON-RPC request
            var requestBody = await ReadRequestBody(context.Request);
            var mcpRequest = JsonConvert.DeserializeObject<MCPRequest>(requestBody);

            // Validate
            if (mcpRequest.JsonRpc != "2.0") {
                await SendErrorResponse(context, -32600, "Invalid JSON-RPC version");
                return;
            }

            // Route to tool
            MCPResponse response = mcpRequest.Method switch {
                "tools/list" => GetToolRegistry(),
                "tools/call" => await ExecuteTool(mcpRequest),
                _ => MCPResponse.Error(-32601, "Method not found")
            };

            // Send response
            await SendJsonResponse(context, response);
        } catch (Exception ex) {
            _logger.Error("MCP request handling failed", "MCP_SERVER", ex);
            await SendErrorResponse(context, -32603, "Internal error");
        }
    }

    private MCPResponse GetToolRegistry() {
        var tools = new[] {
            new MCPTool {
                Name = "get_revit_model_info",
                Description = "Retrieves project metadata (title, version, location)",
                InputSchema = new {
                    type = "object",
                    properties = new { },
                    required = new string[] { }
                }
            },
            new MCPTool {
                Name = "list_levels",
                Description = "Returns all levels and their elevations",
                InputSchema = new {
                    type = "object",
                    properties = new { },
                    required = new string[] { }
                }
            },
            new MCPTool {
                Name = "auto_tag_elements",
                Description = "Creates tags for specified elements",
                InputSchema = new {
                    type = "object",
                    properties = new {
                        category = new { type = "string", description = "Element category (Doors, Walls, etc.)" },
                        scope = new { type = "string", description = "Scope (current_view, level_name, selection)" },
                        tag_type = new { type = "string", description = "Tag type name" },
                        leader = new { type = "boolean", description = "Use leader line" }
                    },
                    required = new[] { "category", "scope", "tag_type" }
                }
            }
        };

        return MCPResponse.Success(new { tools });
    }

    private async Task<MCPResponse> ExecuteTool(MCPRequest request) {
        var toolName = request.Params["name"].ToString();
        var arguments = request.Params["arguments"] as JObject;

        // Safety validation
        var action = ConvertToRevitAction(toolName, arguments);
        var validation = _validator.ValidateOperation(action);
        if (!validation.IsApproved) {
            return MCPResponse.Error(-32602, validation.ErrorMessage);
        }

        // Execute via ExternalEvent (thread-safe)
        var result = await _eventHandler.ExecuteAsync(action);

        return MCPResponse.Success(new { result });
    }

    private RevitAction ConvertToRevitAction(string toolName, JObject arguments) {
        return toolName switch {
            "auto_tag_elements" => new RevitAction {
                Operation = "auto_tag",
                Targets = new Dictionary<string, object> {
                    ["category"] = arguments["category"].ToString(),
                    ["scope"] = arguments["scope"].ToString()
                },
                Parameters = new Dictionary<string, object> {
                    ["tag_type"] = arguments["tag_type"].ToString(),
                    ["leader"] = arguments.Value<bool>("leader")
                }
            },
            _ => throw new ArgumentException($"Unknown tool: {toolName}")
        };
    }
}

// Models/MCPRequest.cs
public class MCPRequest {
    [JsonProperty("jsonrpc")]
    public string JsonRpc { get; set; }

    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("method")]
    public string Method { get; set; }

    [JsonProperty("params")]
    public Dictionary<string, object> Params { get; set; }
}

public class MCPResponse {
    [JsonProperty("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("result")]
    public object Result { get; set; }

    [JsonProperty("error")]
    public MCPError Error { get; set; }

    public static MCPResponse Success(object result) => new MCPResponse { Result = result };
    public static MCPResponse Error(int code, string message) => new MCPResponse {
        Error = new MCPError { Code = code, Message = message }
    };
}

public class MCPError {
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}
```

**Integration with Existing Services:**
```csharp
// Application.cs - Startup
public class Application : IExternalApplication {
    private MCPServer _mcpServer;

    public Result OnStartup(UIControlledApplication application) {
        // ... existing startup code ...

        // Start MCP server
        _mcpServer = new MCPServer(_claudeService, _eventHandler, _validator, _tagService);
        _mcpServer.Start(port: 48884);

        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application) {
        _mcpServer?.Stop();
        return Result.Succeeded;
    }
}
```

**Testing MCP Endpoint:**
```bash
# Test MCP server with curl
curl -X POST http://localhost:48884/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/list",
    "params": {}
  }'

# Expected response:
# {
#   "jsonrpc": "2.0",
#   "id": 1,
#   "result": {
#     "tools": [
#       {
#         "name": "get_revit_model_info",
#         "description": "Retrieves project metadata",
#         ...
#       }
#     ]
#   }
# }

# Test auto-tag tool
curl -X POST http://localhost:48884/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/call",
    "params": {
      "name": "auto_tag_elements",
      "arguments": {
        "category": "Doors",
        "scope": "current_view",
        "tag_type": "Door Tag",
        "leader": false
      }
    }
  }'
```

### Testing Strategy

**Unit Tests:**
```csharp
[TestMethod]
public void ConvertToRevitAction_ValidAutoTagRequest_CreatesAction() {
    var server = new MCPServer(mockClaude, mockEvent, mockValidator, mockTagService);
    var arguments = JObject.Parse(@"{
        ""category"": ""Doors"",
        ""scope"": ""current_view"",
        ""tag_type"": ""Door Tag"",
        ""leader"": false
    }");

    var action = server.ConvertToRevitAction("auto_tag_elements", arguments);

    Assert.AreEqual("auto_tag", action.Operation);
    Assert.AreEqual("Doors", action.Targets["category"]);
    Assert.AreEqual("Door Tag", action.Parameters["tag_type"]);
}

[TestMethod]
public void GetToolRegistry_ReturnsAllTools() {
    var server = new MCPServer(mockClaude, mockEvent, mockValidator, mockTagService);

    var response = server.GetToolRegistry();

    var tools = response.Result["tools"] as MCPTool[];
    Assert.IsTrue(tools.Any(t => t.Name == "auto_tag_elements"));
    Assert.IsTrue(tools.Any(t => t.Name == "get_revit_model_info"));
}
```

**Integration Tests:**
```csharp
[TestMethod]
public async Task MCPServer_EndToEnd_AutoTagRequest_CreatesTagsSuccessfully() {
    // Arrange: Start MCP server with mock Revit environment
    var server = new MCPServer(mockClaude, mockEvent, mockValidator, mockTagService);
    server.Start(port: 48885); // Use different port for testing

    var client = new HttpClient();
    var request = new {
        jsonrpc = "2.0",
        id = 1,
        method = "tools/call",
        @params = new {
            name = "auto_tag_elements",
            arguments = new {
                category = "Doors",
                scope = "current_view",
                tag_type = "Door Tag",
                leader = false
            }
        }
    };

    // Act: Send MCP request
    var response = await client.PostAsync(
        "http://localhost:48885/mcp",
        new StringContent(JsonConvert.SerializeObject(request))
    );

    // Assert: Verify response and tags created
    var responseBody = await response.Content.ReadAsStringAsync();
    var mcpResponse = JsonConvert.DeserializeObject<MCPResponse>(responseBody);

    Assert.IsNotNull(mcpResponse.Result);
    Assert.IsNull(mcpResponse.Error);
    // Verify mockTagService.CreateTagsAsync was called
    Assert.IsTrue(mockTagService.CreateTagsCalled);
}
```

### Definition of Done
- [ ] MCP server starts on Revit startup, listens on localhost:48884
- [ ] Tool registry exposed via `tools/list` endpoint
- [ ] Auto-tag tool callable via `tools/call` with JSON-RPC
- [ ] Requests validated and routed to existing services
- [ ] Errors returned as proper JSON-RPC error codes
- [ ] Unit tests verify request parsing and tool routing
- [ ] Integration tests confirm end-to-end MCP workflow
- [ ] Documentation: MCP usage guide added to README
- [ ] Future-proofing: Architecture supports adding new tools easily

**Strategic Value:**
This story positions RevitAI as MCP-native, enabling future integrations with Claude Desktop, Cursor, Windsurf, and other MCP-compatible tools. Research (Task 1) identified MCP as the emerging standard, and this implementation ensures RevitAI can interoperate with the broader AI tool ecosystem.

---

## Epic Acceptance Criteria

**Epic 2 is complete when:**

1. **Auto-Tagging Works End-to-End:**
   - User enters natural language prompt (Hebrew or English)
   - Claude parses intent accurately (>90% success rate)
   - Safety validator enforces scope limits and allowlist
   - Tag placement engine calculates collision-free positions
   - Preview dialog shows proposed tags with visual graphics
   - User confirms → tags created atomically in Revit
   - Audit log captures operation details

2. **Quality Standards Met:**
   - 80+ unit tests pass (parsing, validation, placement logic)
   - 20+ integration tests pass (mocked Revit API workflows)
   - Code coverage >80% for new services
   - No P0 bugs (crashes, data corruption)
   - Performance: Tag 500 elements in <30 seconds

3. **MCP Compatibility Layer Operational:**
   - MCP server starts automatically with Revit
   - Tool registry lists 4+ tools (model info, levels, elements, auto-tag)
   - External MCP clients can call auto-tag successfully
   - JSON-RPC protocol implemented correctly

4. **Documentation Complete:**
   - Architecture.md updated with Epic 2 decisions
   - CLAUDE.md updated with auto-tagging workflow
   - README includes usage examples and MCP guide
   - Inline code comments for complex algorithms

5. **User Validation:**
   - Tested on real project (not just test fixtures)
   - At least 1 architect successfully uses auto-tagging
   - Feedback collected and documented for Epic 3 planning

---

## Research Attribution

This refactored Epic 2 incorporates findings from three comprehensive research reports:

1. **Task 1: MCP Server Landscape Analysis** (2025-11-20)
   - Identified MCP as emerging standard (49 GitHub stars, active development)
   - Three architectural patterns: Live Session Agent (Node A - matches RevitAI), File-Based (Node B - rejected), Platform-Hub (Node C - future)
   - Operations inventory: auto-tagging prioritized by industry

2. **Task 2: PyRevit+LLM Integration Analysis** (2025-11-20)
   - Market validation: 3 commercial products (ArchiLabs $99/mo, BIMLOGIQ $35/mo, DWD AI Assistant)
   - Gap analysis: No open-source solution with MCP+RAG hybrid + local LLM support
   - User pain points: Auto-tagging #1 request ("Users spend days manually tagging")

3. **Task 3: Revit API Testing Strategies** (2025-11-20)
   - Testing pyramid inverted in Revit (70% E2E, 20% integration, 10% unit)
   - SIL architecture (Layer 1: Pure logic, Layer 2: Revit API wrapper, Layer 3: E2E)
   - Studio Tema case study: Risk aversion (20,000 ILS insurance), mandate for sandbox testing
   - Recommendation: Start with "Read-Only/Annotation" tasks (lower blast radius)

**Strategic Alignment:**
By prioritizing auto-tagging over dimension automation, Epic 2 aligns with:
- **Market demand** (commercial products all prioritize tagging)
- **Risk mitigation** (annotation tasks safer than geometric modifications)
- **User trust** (demonstrate value before asking for higher-risk permissions)
- **Testing feasibility** (simpler to test tag placement than dimension collision avoidance)

---

_Generated: 2025-11-20_
_Status: Draft (Pending Review & Approval)_
_Next Steps: Review with team, finalize story estimates, begin Story 2.1 implementation_
