

# **Report on Architectures for Quality Assurance and API Testing Strategies in the Revit Ecosystem**

## **Executive Summary**

The domain of computational design and Building Information Modeling (BIM) software development stands at a critical juncture. As the industry transitions from monolithic, desktop-centric workflows toward cloud-enabled, data-driven ecosystems, the methodologies for verifying software quality—specifically within the Autodesk Revit API environment—are being forced to evolve. This report provides an exhaustive analysis of the current state of Revit API testing, synthesizing advanced research into interoperability, emerging artificial intelligence (AI) protocols, and real-world industry constraints.

The central thesis of this investigation is that the traditional "Testing Pyramid"—a staple of general software engineering—is fundamentally inverted and often broken within the Revit context due to the "Revit API Bottleneck." This bottleneck, characterized by the tight coupling of business logic to the proprietary Revit database and the necessity of a live runtime session, has historically made rigorous Unit and Integration testing prohibitively difficult. Consequently, the industry has relied on brittle, manual End-to-End (E2E) testing or risky "live" testing on production models.

However, the research identifies a seismic shift driven by three emerging paradigms:

1. **The shift from "Scripted" to "Agentic" Automation:** The rise of Large Language Models (LLMs) and Visual AI allows for "Intent-Based Testing" that is resilient to UI changes, moving beyond the fragility of coordinate-based automation.  
2. **The adoption of Abstraction Layers:** Technologies like the Model Context Protocol (MCP) and Knowledge Graphs are decoupling testing logic from the Revit runtime, enabling a form of "virtualized" testing that mimics standard unit testing speeds.  
3. **The "Platform-Hub" Bypass:** The move to cloud-based data hubs (such as Speckle) allows developers to test data integrity via standard APIs (GraphQL), effectively bypassing the constraints of the desktop application entirely.

This report also highlights the critical "Human-in-the-Loop" and "Audit Loop" strategies essential for mitigating risk in professional practice. As evidenced by case studies of architectural firms, the fear of automation-induced data corruption—and the associated financial liability—dictates a strategy of "Read-Only Validation" and strict sandboxing before any "Write" automation is deployed. The following sections detail the theoretical underpinnings, technical architectures, and strategic implementations of these findings.

## **I. The Theoretical Framework of Quality Assurance in BIM**

To understand the specific challenges of testing in the Revit environment, one must first deconstruct the general principles of software quality assurance and analyze why they fail when applied to BIM. The "Testing Pyramid," a concept foundational to modern DevOps, provides the necessary lens for this analysis.

### **1.1 The Traditional Testing Pyramid and the "BIM Inversion"**

In standard software development, the Testing Pyramid dictates a hierarchy of test types to maximize efficiency and return on investment (ROI). At the base of the pyramid lies **Unit Testing**, representing the largest volume of tests. These are low-level, fast, and inexpensive verifications of individual functions or methods in isolation.1 Above this sits **Integration Testing**, which verifies that different modules or services communicate correctly. At the apex is **End-to-End (E2E) Testing**, which simulates user workflows in a production-like environment. E2E tests are valuable but notoriously slow, expensive to run, and brittle.1

In the context of Revit API development, this pyramid is frequently inverted. The "Base"—Unit Testing—is vanishingly small or non-existent in many projects. This is due to the monolithic architecture of Revit. In a standard web application, a "User" object is a simple data structure that can be instantiated in memory instantly. In Revit, a "Wall" object cannot exist without a "Document," which cannot exist without a "Application" instance, which requires the entire Revit executable to be loaded into memory. This dependency chain makes true "isolation" of code—the prerequisite for unit testing—exceptionally difficult without sophisticated mocking frameworks.2

Consequently, developers are forced to rely heavily on the "Apex"—E2E testing. They write scripts that launch Revit, open a model, perform an action, and check the result. This is the "Inverted Pyramid" or "Ice Cream Cone" anti-pattern. It leads to slow feedback loops, where a developer might wait 10 minutes for Revit to launch just to verify a simple calculation, drastically reducing developer velocity and discouraging frequent testing.

### **1.2 The Semantics of Verification: "Pass/Fail" vs. "Data Fidelity"**

A further complication in BIM testing is the nature of "success." In standard software, a test might check if 2 \+ 2 \= 4\. In BIM, a test might check if "The model was exported to IFC." However, a binary "True/False" result is insufficient. The research into LLM intervention in Revit data export highlights the concept of the "Import/Export Loop" and its inherent "Semantic Degradation".3

When data moves from Revit to an open format like IFC, and potentially back again, it undergoes transformation. "Parametric Families" may become "In-Place Families" (dumb geometry), and critical metadata like fire ratings or material properties may be stripped.3 A test that simply checks "Did the file export?" would pass, but the *fidelity* of the data has failed. Therefore, testing strategies in Revit must move beyond simple execution checks and embrace "Semantic Validation" or "Auditing." This requires examining the *content* of the data artifacts for corruption, logic errors, or loss of intelligence, a process that is far more complex than standard functional testing.

### **1.3 The Role of "Intent-Based" Validation**

The fragility of traditional automation scripts in Revit—often reliant on specific button locations or UI element IDs—has created a maintenance nightmare. A minor update to the Revit UI by Autodesk can break an entire test suite. The research suggests a move toward **Intent-Based Automation**.1 In this paradigm, the test does not define *how* to execute a task (e.g., "Click pixel 200,400") but rather *what* the intent is (e.g., "Tag all walls on Level 1").

This shift is enabled by AI agents equipped with Computer Vision and Natural Language Processing (NLP). These agents can "see" the interface like a human, recognizing the "Tag All" button regardless of its position.1 This aligns with the "Agentic" workflows emerging in the broader industry, where the focus shifts from scripting rigid procedures to defining goals and allowing intelligent systems to navigate the execution path. This evolution is critical for creating resilient E2E tests in the volatile environment of proprietary desktop software.

## **II. The "Broken Loop": Challenges in Revit Data Fidelity**

The analysis of current interoperability workflows reveals a fundamental structural weakness in the Revit ecosystem that profoundly impacts testing strategies: the "Broken Import/Export Loop." Understanding this failure mode is essential, as it defines the "Blast Radius" of any automated testing or intervention.

### **2.1 The Architecture of Data Loss**

The concept of "Round-Tripping"—exporting data from Revit, modifying it externally, and re-importing it—is the theoretical cornerstone of OpenBIM. However, the research indicates that for Revit, this is a "well-documented point of failure" caused by an architectural misalignment between Revit's internal proprietary schema and open standards like IFC.3

The primary failure modes identified are:

1. **Semantic Degradation:** Intelligent, parametric elements (e.g., a "Family Instance" of a door with width and height parameters) are often converted into "DirectShapes" or "In-Place Families" upon re-import. These are geometric blobs that look correct but lack the "intelligence" to be scheduled or modified parametrically.  
2. **Data Stripping:** Attributes such as thermal properties, phasing, or custom user parameters are frequently lost during the translation process.  
3. **Geometric Corruption:** Discrepancies in coordinate systems (Project Base Point vs. Survey Point) can lead to geometry being imported in the wrong location or with distorted topology.3

### **2.2 The Implication for Automated Testing**

This "Broken Loop" implies that any testing strategy that relies on "Round-Tripping" as a validation mechanism is inherently flawed. For example, a test that verifies a plugin by "Exporting to IFC, modifying a property, re-importing, and checking the property" is likely to fail not because the plugin is broken, but because the *process* of re-importing is lossy.

This necessitates a strategic pivot in testing methodology. Instead of testing the "Loop," sophisticated strategies focus on **"The Audit."** The "Audit Loop" methodology described in the research 3 positions the testing agent (often an LLM) as a validator that checks data *at the point of exit* or *at the point of entry*, but does not attempt to merge it. For instance, an automated test might generate a compliance report based on the exported IFC file, validating that the data *in the file* is correct. This "Read-Only" approach avoids the risks associated with re-importing corrupted geometry into a production model.

### **2.3 The "In-Place Family" Anomaly**

A specific, recurring issue in Revit automation is the proliferation of "In-Place Families." These are unique, one-off geometry creations that bloat the model size and degrade performance. Automated testing scripts that generate geometry must be rigorously tested to ensure they are instantiating standard *Family Types* rather than creating new *In-Place* geometry for every instance.

Current AI interventions struggle significantly with this. The research notes that while LLMs can generate scripts to "create geometry," they often lack the contextual awareness to search the internal library for an existing family and place an instance of it.3 Instead, they default to creating raw geometry. A robust testing strategy must therefore include "Model Hygiene Checks" that count the number of In-Place families before and after a test run to detect this specific type of "Semantic Pollution."

## **III. Emerging Architectures for Intervention and Testing**

To overcome the "Revit API Bottleneck" and the limitations of the "Broken Loop," the industry is adopting new architectures that abstract, virtualization, or bypass the Revit application. These architectures form the basis of modern, scalable testing strategies.

### **3.1 The Model Context Protocol (MCP) as a Universal Adapter**

One of the most significant developments identified in the research is the **Model Context Protocol (MCP)**. This open standard acts as a "USB-C port for AI," providing a universal interface for connecting LLMs and external agents to local applications like Revit.3

In the context of testing, MCP serves as a powerful abstraction layer. The revit-mcp-python implementation functions as a local server running inside the Revit process. It exposes a set of standardized "Tools"—such as get\_model\_info, list\_elements, or create\_wall—accessible via JSON requests.

**Implication for Testing:** This allows developers to write test suites that are decoupled from the Revit API's C\# or Python specifics. A test case becomes a sequence of JSON prompts:

1. Request: { "tool": "create\_level", "elevation": 0 }  
2. Response: { "status": "success", "id": 1234 }  
3. Request: { "tool": "get\_element", "id": 1234 }  
4. Validation: Assert elevation \== 0

This architecture enables "Mocking" on a grand scale. During development, the "MCP Host" can be swapped for a simple mock server that returns pre-canned JSON responses, allowing the logic of the agent or plugin to be tested *without* launching Revit. Only the final integration pass requires the live revit-mcp-python server.

### **3.2 Knowledge Graphs (KG) for Relational Validation**

Another emerging architecture involves transforming the Revit model into a **Knowledge Graph (KG)** or Labeled Property Graph (LPG).3 This approach addresses the difficulty of testing complex relationships in a flat database.

In a KG, every element (Wall, Door, Room) is a node, and every relationship (Hosts, Bounds, Connects To) is an edge. This structure allows for "Deep Relational Querying." A test can easily verify complex logic, such as "Ensure all Rooms named 'Office' contain at least one Desk and one Chair," using a query language like Cypher or SPARQL.

**Implication for Testing:** This architecture shifts the "Test" from a procedural script to a declarative query. Instead of writing a loop that iterates through all rooms and checks content (slow and buggy), the test simply executes a graph query against the exported data. If the query returns any results (violating the rule), the test fails. This is particularly effective for verifying the semantic integrity of large models.

### **3.3 The "Platform-Hub" Bypass (The Speckle Method)**

The most radical strategy identified is to bypass the Revit API and file formats entirely in favor of a **Cloud-Based Data Hub**, such as Speckle.3 In this model, Revit is treated merely as an "Authoring Interface." The "Source of Truth" is a granular, object-based database in the cloud.

**Implication for Testing:** This solves the "Geometry Problem" and the "Access Problem" simultaneously.

* **Geometry:** The Hub's connectors handle the heavy lifting of translating Revit geometry into a web-friendly format. The test does not need to process raw mesh data.  
* **Access:** The Hub exposes the model data via a standard GraphQL or REST API.

Tests can be written in any language (JavaScript, Python) and run on any standard CI/CD runner (GitHub Actions, GitLab CI). They query the Hub directly: "Get all objects of type 'Wall' from the latest commit and verify parameter 'FireRating' \> 0." This effectively turns "BIM Testing" into "Web API Testing," a domain with mature, high-speed tooling. This is described in the research as the "Long-Term Strategic Recommendation" for the industry.3

## **IV. Automated Testing Strategies and Methodologies**

Based on the theoretical frameworks and emerging architectures, we can categorize the specific testing strategies available to Revit developers today. These range from legacy methods to cutting-edge AI implementations.

### **4.1 Strategy 1: Live Session Automation (The "Intent-Based" Evolution)**

**Overview:** This strategy involves automating the Revit application itself. Historically, this was done using "Journal Files" or coordinate-based mouse clickers, both of which were notoriously brittle. The modern evolution utilizes "Intent-Based Automation" powered by Visual AI.1

**Mechanism:** An AI agent (using tools like Applitools or custom vision models) "looks" at the Revit window. The test instruction is high-level: "Go to the Manage tab and click Project Units." The agent identifies the visual elements and executes the interaction.

**Advantages:**

* **True Fidelity:** It tests the actual user experience, including dialog boxes, warnings, and rendering issues that API tests might miss.  
* **Resilience:** Visual AI is tolerant of UI changes (e.g., a button moving slightly) that would break selector-based scripts.1

**Disadvantages:**

* **Speed:** It is bound by the speed of the Revit UI, which is slow.  
* **Resource Intensity:** Requires a full GPU-enabled machine to run effectively.

**Best Use Case:** Final "Smoke Tests" before a release to ensure no UI-blocking bugs exist.

### **4.2 Strategy 2: Decoupled Logic (The "Unit Testing" Ideal)**

**Overview:** This strategy focuses on structuring code to isolate "Business Logic" from "Revit API Calls." This is the standard "Separation of Concerns" principle applied to BIM.1

**Mechanism:**

* **Bad Code:** A function takes a Revit.DB.Wall, extracts its height, multiplies it by length to get area, and returns the cost. This cannot be tested without Revit.  
* **Good Code:** A function takes a float height and float length. A separate "Adapter" function extracts these values from the Revit.DB.Wall and passes them to the logic function.

**Advantages:**

* **Speed:** The logic function can be tested in milliseconds using standard frameworks (NUnit, PyTest).  
* **Portability:** Tests run on any machine, including lightweight cloud CI containers.

**Disadvantages:**

* **Coverage Gap:** It does not test the "Adapter"—i.e., it doesn't verify if you extracted the height correctly, only that you calculated the cost correctly *given* the height.

**Best Use Case:** Testing complex mathematical algorithms, data validation rules, or external API integrations (e.g., pricing calculators).

### **4.3 Strategy 3: The Audit Loop (Pre/Post Process Validation)**

**Overview:** Recognizing the "Broken Loop" of data fidelity, this strategy validates the *state* of the model rather than the *process* of the tool.3

**Mechanism:**

* **Pre-Export Audit:** An LLM or script scans the model against a "BIM Execution Plan" (BEP). It checks for naming conventions, missing parameters, or classification errors *before* the tool runs.  
* **Post-Import Audit:** After data is imported (e.g., from an IFC file), the auditor scans the model again to generate a "Diff Report" or "Compliance Report."

**Advantages:**

* **Risk Mitigation:** It acts as a safety net, catching "Silent Failures" like data stripping.  
* **Human-in-the-Loop:** It produces reports for human review, aligning with the cautious approach of firms like Studio Tema.4

**Best Use Case:** Data exchange workflows (Revit to IFC, Revit to COBie) where data integrity is paramount.

### **4.4 Strategy 4: Synthetic Data and Just-in-Time Mocks**

**Overview:** Using Generative AI to create test data on the fly, solving the "Setup Complexity" problem of creating Revit models for testing.1

**Mechanism:** Instead of maintaining a library of huge .rvt files for testing, developers use LLMs to generate synthetic JSON representations of Revit elements. A prompt might be: "Generate a JSON object representing a Revit Room with an area of 50sqm and a perimeter of 30m." This data is then fed into the "Decoupled Logic" tests.

**Advantages:**

* **Scalability:** Can generate thousands of variations (edge cases, invalid inputs) instantly.  
* **Privacy:** Avoids using client data for testing.

**Best Use Case:** Stress testing algorithms and verifying error handling logic.

## **V. Case Studies and Real-World Implementation**

The transition from theory to practice is fraught with challenges. The following case studies, derived from the research, illustrate how these strategies are (or are not) being implemented in the real world.

### **5.1 Case Study: Studio Tema – The Economics of Risk and "Sandboxing"**

**Context:** The "Studio Tema" proceedings 4 provide a granular look at a mid-sized architecture firm's grappling with AI and automation. The firm is interested in efficiency (auto-tagging, dimensioning) but is paralyzed by the risk of automation errors.

**The Constraints:**

* **Financial Liability:** The firm explicitly quantified the risk of automation failure at **20,000 ILS** (the insurance deductible). This fear dictates their testing strategy: they cannot afford a "runaway" script that deletes or corrupts elements in a production model.  
* **Licensing and Infrastructure:** The firm operates with a mix of **Full Revit Licenses** (costing \~100,000 ILS/year) and **Revit Lite**. This creates a heterogeneous testing environment. Scripts that rely on API features not present in Revit Lite (which has no native API support for plugins, though some external tools interact via other means) will fail for a subset of users.  
* **The "Sandbox" Mandate:** To mitigate risk, the firm mandated the creation of a distinct "Internal Sandbox Environment." This is a non-production Revit setup where all new automation tools—specifically mentioning "Auto-tagging" and "Internal Dimensions"—must be proven before deployment.4

**Testing Strategy Implemented:**

* **Human-Gated Automation:** They rejected "Full Automation" in favor of "Assisted Automation." The AI suggests a tag location; a human confirms it. This reduces the need for 100% rigorous E2E testing because the human is the final "test" step.  
* **Focus on Read-Only/Annotation:** They prioritized tasks (Tags, Dimensions) that add data *on top* of the model rather than modifying the building geometry itself. This lowers the "Blast Radius" of a bug.

### **5.2 Case Study: BIMgent and Text2BIM – The Agentic Frontier**

**Context:** Academic and experimental frameworks exploring the limits of LLM control over BIM.3

**The Innovation:** These frameworks utilize "Multi-Agent" systems. One agent plans the task ("I need to build a house"), and another agent executes the specific API calls or UI clicks.

**Testing Strategy Implemented:**

* **Self-Correction:** The agents employ a "Reflect and Verify" loop. After executing a command (e.g., "Create Wall"), the agent queries the model: "Did a wall appear?" If not, it attempts to "Self-Heal" by trying a different method or adjusting parameters.1  
* **Usage of Documentation:** The agents are connected via RAG to the Revit API documentation. If a test fails due to an API error, the agent can "read" the error message, look up the documentation, and rewrite its own code to fix the error dynamically.

### **5.3 Case Study: Monday.com Integration – Testing the "Business Logic"**

**Context:** Firms integrating Revit with project management tools like monday.com to track design progress.5

**The Workflow:**

1. **Trigger:** A Dynamo script marks a design phase as "Complete" in Revit.  
2. **Action:** Zapier/Make receives a webhook and creates a task in monday.com.  
3. **Intelligence:** AI Blocks in monday.com categorize the task or summarize attached RFI data.5

**Testing Strategy Implemented:**

* **Integration Testing via Orchestration:** The "Test" here is finding out if the webhook fired. The validation happens outside Revit, in the monday.com dashboard.  
* **Low-Code Verification:** Using the "Test Run" features in Zapier/Make to simulate payloads. This allows the logic of the integration to be verified without opening Revit, by manually sending the JSON payload that *would* have come from Revit.

## **VI. CI/CD and The Future of BIM DevOps**

The integration of these strategies into a Continuous Integration/Continuous Delivery (CI/CD) pipeline is the final frontier of BIM testing.

### **6.1 The "Shift-Left" Paradigm in BIM**

The software industry is moving toward "Shift-Left" testing—testing as early as possible in the development cycle.1 For Revit, this means running tests on the "Code Commit" rather than waiting for a "Nightly Build."

**Implementation:**

* **Pre-Commit Hooks:** Scripts that run locally on the developer's machine before code is pushed. These run the "Decoupled Logic" unit tests.  
* **Pull Request (PR) Checks:** When code is pushed to GitHub/GitLab, a CI runner executes a broader suite of tests. Since standard runners (like GitHub Actions) cannot run Revit, these tests are limited to:  
  1. Unit Tests (Logic).  
  2. Linting (Code Style).  
  3. Mocked Integration Tests (using revit-mcp-python mocks).

### **6.2 The "Cloud Licensing" Bottleneck**

A major barrier to true CI/CD is the licensing model. As noted in the Studio Tema case 4, Revit licenses are expensive and node-locked. Running a "Headless Revit" in a cloud container for testing purposes is technically difficult and legally complex regarding Autodesk's Terms of Service.

**Workarounds:**

* **Self-Hosted Runners:** Firms set up a physical machine (the "Build Box") in their office that has a valid Revit license. The cloud CI system (GitHub) sends a signal to this local machine to pull the code and run the "Live Session" tests.  
* **Autodesk Platform Services (APS) Design Automation:** Using Autodesk's cloud API to run scripts. This is the "Official" way to run headless Revit code, but it has a cost per processing hour and a different API surface than the desktop application, making it an imperfect proxy for UI-based add-ins.

### **6.3 Human-in-the-Loop Governance**

Given the risks, "Governance" is as important as "Automation".1 The "Human-in-the-Loop" remains the ultimate gatekeeper.

* **The "Approval" Step:** In the CI/CD pipeline, deployment to production is never automatic. It requires a manual approval step, usually after the human reviewer has examined the "Audit Report" generated by the automated tests.  
* **Role of the BIM Manager:** The BIM Manager transforms into the "QA Lead," responsible not for manually clicking buttons, but for reviewing the automated "Diff Reports" and "Compliance Checks" before authorizing a plugin update.

## **VII. Strategic Recommendations and Conclusion**

The landscape of Revit API testing is shifting from a manual, labor-intensive burden to a sophisticated, multi-layered architectural challenge. The "Revit API Bottleneck" is being eroded by abstraction layers (MCP), cloud bypasses (Speckle), and intelligent agents.

### **7.1 Recommendations**

1. **Immediate Term (The "Audit" Defense):** Do not strive for full automation immediately. Implement "Read-Only" audit scripts that validate model health using the **Knowledge Graph** or **LLM-RAG** approaches. This provides immediate value with zero risk of data corruption.3  
2. **Medium Term (The "MCP" Abstraction):** Refactor existing testing suites to use the **Model Context Protocol**. Even if you are not using AI agents yet, the standardized JSON interface of MCP allows you to decouple your tests from the Revit version and enables easier mocking.3  
3. **Long Term (The "Platform" Pivot):** Plan for a future where testing happens in the cloud. Investigate **Speckle** or **APS** as the primary venue for data integrity testing. Treat the Revit application as just one of many "clients" that feed the central data hub.3  
4. **Operational Hygiene (The "Sandbox"):** As demonstrated by Studio Tema, rigorously enforce a "Sandbox Policy." Maintain a dedicated environment for testing that mirrors the diverse licensing landscape (Full vs. Lite) of the production environment.4

### **7.2 Conclusion**

The question of "how to test Revit add-ins without full launches" has evolved. The answer is no longer just "use mocks." The answer is to fundamentally restructure the architecture of BIM data flow. By treating the BIM model as a database rather than a file, and by employing AI agents as semantic auditors rather than blind click-bots, the industry can finally achieve the velocity and reliability that defines modern software engineering. The future of Revit testing is not inside Revit—it is in the abstraction layers that surround it.

### **Open Questions for Future Research**

* **Round-Trip Fidelity:** Can the "Broken Loop" of IFC ever be fully repaired, or is the industry destined to abandon file-based exchange for API-based hubs entirely? 3  
* **AI Trust:** As we offload testing to AI agents, how do we "Verify the Verifier"? What frameworks will exist to audit the logic of the AI agents themselves? 1  
* **Legal Frameworks:** Will Autodesk evolve its licensing model to permit ephemeral, cloud-based testing containers, or will the "Build Box" remain a physical necessity for AEC firms? 4

---

**Table 1: Comparative Analysis of Revit Testing Strategies**

| Strategy | Primary Target | Setup Complexity | Speed | Fidelity | CI/CD Suitability |
| :---- | :---- | :---- | :---- | :---- | :---- |
| **Live Session Automation** | UI & Command Execution | High (Requires GPU/License) | Low | High (True User Experience) | Low |
| **Decoupled Logic (Unit)** | Algorithms & Data Processing | Low | Very High | Low (No API Interaction) | High |
| **Platform-Hub (Speckle)** | Data Integrity & Semantics | Medium (Requires Server) | High | Medium (Dependent on Connector) | Very High |
| **MCP Integration** | Interaction & Querying | High (Requires Agent Setup) | Medium | High (Virtualized API) | Medium |
| **Audit Loop** | Data Compliance | Medium | Medium | High (Semantic Validation) | Medium |

#### **Works cited**

1. AI for Software Testing , [https://drive.google.com/open?id=1lPhYK7IQ3tev8dBSwCichCChxJNKhoOGT5fMIU3IA2o](https://drive.google.com/open?id=1lPhYK7IQ3tev8dBSwCichCChxJNKhoOGT5fMIU3IA2o)  
2. AI for Software Testing, [https://drive.google.com/open?id=13UEYK3nhaRuF21Qk6dACeLI8BucjQgFY7ObQORHMI7w](https://drive.google.com/open?id=13UEYK3nhaRuF21Qk6dACeLI8BucjQgFY7ObQORHMI7w)  
3. LLM Intervention in Revit Data Export, [https://drive.google.com/open?id=1vwH-kGG5WdxLmrBGbM5YdgmzzvAO6vfToedVcM18gwM](https://drive.google.com/open?id=1vwH-kGG5WdxLmrBGbM5YdgmzzvAO6vfToedVcM18gwM)  
4. Pery \- Tomer-summary-2025-11-06T21-47-52.523Z.docx, [https://drive.google.com/open?id=1sse\_pqp2k7lBPxcw10bTL\_7MzaFB\_mhf](https://drive.google.com/open?id=1sse_pqp2k7lBPxcw10bTL_7MzaFB_mhf)  
5. AI באדריכלות: כלים ויישומים, [https://drive.google.com/open?id=1Ul\_JkQl53NsVgKF2aDyfY7qZxuFFjrp\_0IX90VRnBvU](https://drive.google.com/open?id=1Ul_JkQl53NsVgKF2aDyfY7qZxuFFjrp_0IX90VRnBvU)