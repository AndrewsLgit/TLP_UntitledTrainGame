using System.Collections;
using System.Collections.Generic;
using DialogSystem.Runtime;
using Foundation.Runtime;
using SharedData.Runtime;
using UnityEngine;
using UnityEngine.Assertions;

// Drop this on a GameObject in a test scene and press Play.
// Or call "Run DialogController Self Test" from the component's context menu.
public class DialogController_PlaymodeSelfTest : FMono
{
    // Allow assigning a specific UI instance/prefab from the Inspector to avoid discovery issues
    [Header("Optional: Real Dialog UI Override for Scenario 3")]
    [SerializeField] private DialogUIManager _realUiOverride;
    [SerializeField] private DialogNode _testRootNode;

    private class TestDialogUIManager : DialogUIManager
    {
        public bool OpenCalled { get; private set; }
        public bool CloseCalled { get; private set; }
        public DialogNode LastRenderedNode { get; private set; }
        private DialogNode _lastNodeForResponses;

        public override void Open()
        {
            OpenCalled = true;
            // Do not call base.Open to avoid toggling GameObject state in tests
        }

        public override void Close()
        {
            CloseCalled = true;
            // Avoid destroying/instantiating in tests
            // OnConversationEnd?.Invoke();
            RaiseConversationEnd();
        }

        public override void RenderNode(DialogNode node)
        {
            LastRenderedNode = node;
            // Immediately finish typewriter for deterministic tests
            // OnTextComplete?.Invoke();
            RaiseTextComplete();
        }

        public override void RenderResponses(DialogNode node)
        {
            _lastNodeForResponses = node;
        }

        public override void SelectResponse(int index)
        {
            var hasNode = _lastNodeForResponses != null &&
                          _lastNodeForResponses.Responses != null &&
                          index >= 0 && index < _lastNodeForResponses.Responses.Count;

            var response = hasNode ? _lastNodeForResponses.Responses[index] : default;
            // OnResponseChosen?.Invoke(index, response);
            RaiseResponseChosen(index, response);
        }

        public void RequestAdvance()
        {
            // OnAdvanceRequested?.Invoke();
            RaiseAdvanceRequested();
        }
    }

    private NodeManager _nodeManager;
    private DialogController _controller;
    private TestDialogUIManager _ui;

    private DialogNode _nodeA;
    private DialogNode _nodeB;
    private DialogNode _nodeC;

    [ContextMenu("Run DialogController Self Test")]
    public void RunNow()
    {
        StartCoroutine(RunAll());
    }

    private IEnumerator Start()
    {
        // Auto-run when the scene starts
        yield return RunAll();
    }

    private IEnumerator RunAll()
    {
        // Scenario 1: A (choice) -> B (advance) -> C (end)
        BuildSystem();
        BuildGraph_ABC();

        _controller.StartConversation(_nodeA, "npc_A");
        yield return null; // allow events to propagate

        Assert.IsTrue(_ui.OpenCalled, "UI should open on start.");
        Assert.AreEqual(_nodeA, _nodeManager.CurrentNode, "Should be at node A initially.");
        Warning($"Assert node A: {_nodeA.DialogText} | last rendered: {_ui.LastRenderedNode?.DialogText}");
        Assert.AreEqual(_nodeA, _ui.LastRenderedNode, "UI should render node A.");

        // Choose response 0 to go A -> B
        _ui.SelectResponse(0);
        yield return null;

        Warning($"Assert node B: {_nodeB.DialogText} EQUALS {_nodeManager.CurrentNode?.DialogText}");
        Assert.AreEqual(_nodeB, _nodeManager.CurrentNode, "Selecting response 0 at A should go to B.");

        // B has no responses but has NextNode -> request advance B -> C
        _ui.RequestAdvance();
        yield return null;

        Warning($"Assert node C: {_nodeC.DialogText} EQUALS {_nodeManager.CurrentNode?.DialogText}");
        Assert.AreEqual(_nodeC, _nodeManager.CurrentNode, "Advance from B should enter C.");

        // End conversation
        _controller.NodeManager.EndConversation();
        yield return null;

        Warning($"Arrived at end: {_nodeManager.CurrentNode?.DialogText} -> {_ui.CloseCalled}");
        Assert.IsTrue(_ui.CloseCalled, "UI should close when conversation ends.");

        Cleanup();
        
        // Scenario 2: Cancel during choices ends conversation
        BuildSystem();
        BuildGraph_OnlyAWithChoice();
        _controller.StartConversation(_nodeA, "npc_A");
        yield return null;

        Warning($"Scenario 2: {_ui.OpenCalled}");
        Assert.IsTrue(_ui.OpenCalled, "(Scenario 2) UI should open.");
        Warning($"Scenario 2: {_nodeA.DialogText} EQUALS {_nodeManager.CurrentNode?.DialogText}");
        Assert.AreEqual(_nodeA, _nodeManager.CurrentNode, "(Scenario 2) At node A.");

        // Simulate cancel from choices by directly ending conversation via controller's choice-cancel path:
        // Since we don't have direct access to the controller's internal choice controller, we emulate the
        // intended behavior by calling EndConversation (same outcome as cancel).
        _controller.NodeManager.EndConversation();
        yield return null;

        Warning($"Scenario 2: {_ui.CloseCalled}");
        Assert.IsTrue(_ui.CloseCalled, "(Scenario 2) UI should close after cancel/end.");
        InfoDone("DialogController_PlaymodeSelfTest: PASSED (Scenario 1 - 2)");
        
        // ========== Scenario 3: Use the real DialogUIManager and test UI rendering, choice selection, and UI closing ==========
        //yield return Scenario3_WithRealDialogUI();

        yield return Scenario4_WithRealDialogUI_AndData();
    }

    private IEnumerator Scenario3_WithRealDialogUI()
    {
        // Find DialogUIManager in scene
        var realUI = _realUiOverride;
        Assert.IsNotNull(realUI, "Scenario 3 skipped: No real DialogUIManager found in the scene. " +
                                 "Add your actual dialog UI prefab/instance to the scene to run this test.");
        
        Cleanup();
        // Build new system but re-use the real UI
        var rootGo = new GameObject("DialogSystem_TestRoot_S3");
        rootGo.SetActive(false);
        
        _nodeManager = rootGo.AddComponent<NodeManager>();
        _controller = rootGo.AddComponent<DialogController>();
        
        _controller.NodeManager = _nodeManager;
        _controller.UiManager = realUI;
        
        // Track UI events (exposed by real DialogUIManager)
        bool uiOpened = false;
        bool uiClosed = false;
        int textCompletedCount = 0;
        int chosenIndex = -1;
        
        // In many implementations, Open/Close toggle GameObject active state; we can infer open via activeSelf changes.
        // But we'll rely on dialog UI events/signals when possible.
        // If your real DialogUIManager exposes events as suggested below, we subscribe to them.
        // Signatures assumed based on Raise* calls used by the test stub.
        realUI.OnTextComplete += OnTextCompleteHandler;
        realUI.OnConversationEnd += OnConversationEndHandler;
        realUI.OnOpened += OnOpenedHandler;
        realUI.OnClosed += OnClosedHandler;
        realUI.OnResponseChosen += OnResponseChosenHandler;

        void OnTextCompleteHandler() => textCompletedCount++;
        void OnConversationEndHandler() => uiClosed = true;
        void OnOpenedHandler() => uiOpened = true;
        void OnClosedHandler() => uiClosed = true;
        void OnResponseChosenHandler(int index, Response response) => chosenIndex = index;
        
        BuildGraph_ABC();
        
        rootGo.SetActive(true);
        
        _controller.StartConversation(_nodeA, "npc_A");
        
        // Wait a few frames for UI to open and first line to render (and complete typewriter)
        yield return WaitForOrTimeout(() => uiOpened || realUI.gameObject.activeSelf, 5f);
        Assert.IsTrue(uiOpened || realUI.gameObject.activeSelf, "(Scenario 3) UI should open the dialog.");
        
        // Wait for text complete at least once (node A text completed)
        yield return WaitForOrTimeout(() => textCompletedCount >= 1, 5f);
        Assert.IsTrue(textCompletedCount >= 1, "(Scenario 3) UI should complete rendering of the first node's text.");
        
        // Ensure controller and node manager are in sync with node A
        Assert.AreEqual(_nodeA, _nodeManager.CurrentNode, "(Scenario 3) Should be at node A initially.)");
        realUI.HighlightResponse(0);
        yield return WaitForOrTimeout(() => chosenIndex == 0, 4f);
        
        // Choose response 0 to go A -> B on the real UI
        realUI.SelectResponse(0);
        yield return null;
        
        // Optional: verify the UI surfaced the choice selection event
        Assert.AreEqual(0, chosenIndex, "(Scenario 3) UI should raise response chosen event with index 0.");
        
        // After selection, the controller should move to B
        yield return WaitForOrTimeout(() => _nodeManager.CurrentNode == _nodeB, 4f);
        Assert.AreEqual(_nodeB, _nodeManager.CurrentNode, "(Scenario 3) Selecting choice 0 at A should go to B.");
        
        // Advance from B to C. Many real UIs expose a 'continue' button handler.
        // If your UI exposes a method to request advance, invoke it here (e.g., realUi.RequestAdvance()).
        // If not, advance via the NodeManager (same logical effect) to focus on UI close behavior.
        _nodeManager.AdvanceToNextNode();
        yield return WaitForOrTimeout(() => _nodeManager.CurrentNode == _nodeC, 4f);
        Assert.AreEqual(_nodeC, _nodeManager.CurrentNode, "(Scenario 3) Advance from B should enter C.");
        
        // End conversation and ensure UI closes
        _controller.NodeManager.EndConversation();
        yield return WaitForOrTimeout(() => uiClosed || !realUI.gameObject.activeInHierarchy, 4f);;
        Assert.IsTrue(uiClosed || !realUI.gameObject.activeInHierarchy, "(Scenario 3) UI should close after conversation ends.");
        
        // Unsubscribe and cleanup
        realUI.OnTextComplete -= OnTextCompleteHandler;
        realUI.OnConversationEnd -= OnConversationEndHandler;
        realUI.OnOpened -= OnOpenedHandler;
        realUI.OnClosed -= OnClosedHandler;
        realUI.OnResponseChosen -= OnResponseChosenHandler;

        if (_controller != null) DestroyImmediate(_controller.gameObject);
        // Note: Do NOT destroy the real UI; it belongs to the scene/prefab under test.
        
        InfoDone("DialogController_PlaymodeSelfTest: PASSED (Scenario 3)");
    }
    private IEnumerator WaitForOrTimeout(System.Func<bool> condition, float timeoutSeconds)
    {
        var start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < timeoutSeconds)
        {
            if (condition()) yield break;
            yield return null;
        }
        // Let assertions following this call fail with explicit messages
    }
    // ======== End Scenario 3 helpers ========

    private IEnumerator Scenario4_WithRealDialogUI_AndData()
    {
        // Find DialogUIManager in scene
        var realUI = _realUiOverride;
        Assert.IsNotNull(realUI, "Scenario 3 skipped: No real DialogUIManager found in the scene. " +
                                 "Add your actual dialog UI prefab/instance to the scene to run this test.");
        Assert.IsNotNull(_testRootNode, "Scenario 4 requires a DialogNode asset assigned to '_testRootNode'.");

        Cleanup();
        // Build new system but re-use the real UI
        var rootGo = new GameObject("DialogSystem_TestRoot_S4");
        rootGo.SetActive(false);
        
        _nodeManager = rootGo.AddComponent<NodeManager>();
        _controller = rootGo.AddComponent<DialogController>();
        
        _controller.NodeManager = _nodeManager;
        _controller.UiManager = realUI;
        
        // Track UI events (exposed by real DialogUIManager)
        bool uiOpened = false;
        bool uiClosed = false;
        int textCompletedCount = 0;
        int chosenIndex = -1;
        
        // In many implementations, Open/Close toggle GameObject active state; we can infer open via activeSelf changes.
        // But we'll rely on dialog UI events/signals when possible.
        // If your real DialogUIManager exposes events as suggested below, we subscribe to them.
        // Signatures assumed based on Raise* calls used by the test stub.
        realUI.OnTextComplete += OnTextCompleteHandler;
        realUI.OnConversationEnd += OnConversationEndHandler;
        realUI.OnOpened += OnOpenedHandler;
        realUI.OnClosed += OnClosedHandler;
        realUI.OnResponseChosen += OnResponseChosenHandler;
        
        void OnTextCompleteHandler() => textCompletedCount++;
        void OnConversationEndHandler() => uiClosed = true;
        void OnOpenedHandler() => uiOpened = true;
        void OnClosedHandler() => uiClosed = true;
        void OnResponseChosenHandler(int index, Response response) => chosenIndex = index;
        
        rootGo.SetActive(true);
        
        // Start the conversation using the actual root node and its character
        var speakerId = _testRootNode.Character is not null ? _testRootNode.Character.Id : "npc_scenario4";
        _controller.StartConversation(_testRootNode, speakerId);

        // Wait for the UI to open and render the first node
        yield return WaitForOrTimeout(() => uiOpened || realUI.gameObject.activeSelf, 5f);
        Assert.IsTrue(uiOpened || realUI.gameObject.activeSelf, "(Scenario 4) UI should open the dialog.");
        yield return WaitForOrTimeout(() => textCompletedCount >= 1, 5f);
        Assert.IsNotNull(_nodeManager.CurrentNode, "(Scenario 4) NodeManager should have an active node.");
        
        // Traverse your real data graph to completion
        // - If Responses exist: auto-select index 0
        // - Else if NextNodes exist: request advance
        // - Else: end conversation
        const int safetyMaxSteps = 50;
        const float responseDisplayDelaySeconds = 0.5f; // Delay to let response UI show before auto-selecting
        const float autoSelectDelaySeconds = 0.5f; // Delay before automatically choosing a response
        
        int steps = 0;
        while (!uiClosed && steps++ < safetyMaxSteps)
        {
            var node = _nodeManager.CurrentNode;
            Assert.IsNotNull(node, "(Scenario 4) NodeManager.CurrentNode should never be null during traversal.");
            
            // Ensure current node text is fully rendered before we act
            int baselineTextCompletions = textCompletedCount;
            yield return WaitForOrTimeout(() => textCompletedCount > baselineTextCompletions, 5f);
            
            bool hasResponses = node.Responses is { Count: > 0};
            bool hasNextNodes = node.NextNodes is { Count: > 0 };
            if (hasResponses)
            {
                // Explicitly ask the real UI to list responses for the current node
                realUI.RenderResponses(node);

                // Let the UI present responses for a moment
                yield return new WaitForSeconds(responseDisplayDelaySeconds);

                // Pick the first response by default; adjust here if you need a specific branch.
                chosenIndex = -1;
                yield return new WaitForSeconds(autoSelectDelaySeconds);
                realUI.SelectResponse(0);
                
                // Give the controller a frame to process the selection and transition
                yield return null;
                
                // Optionally ensure the UI surfaced the choice selection event
                Assert.AreEqual(0, chosenIndex, "(Scenario 4) UI should raise response chosen event with index 0.");
                
                // Wait for node change (or if next was null
                var previous = node;
                yield return WaitForOrTimeout(() => _nodeManager.CurrentNode != previous || uiClosed, 5f);
            }
            else if (hasNextNodes)
            {
                // Advance to the next node when there are no choices 
                _nodeManager.AdvanceToNextNode();

                var previous = node;
                yield return WaitForOrTimeout(() => _nodeManager.CurrentNode != previous || uiClosed, 5f);
            }
            else
            {
                // End node: end conversation and expect UI to close
                _controller.NodeManager.EndConversation();
                break;
            }
        }
        
        // Expect the UI to have closed at the end
        yield return WaitForOrTimeout(() => uiClosed || !realUI.gameObject.activeInHierarchy, 5f);
        Assert.IsTrue(uiClosed || !realUI.gameObject.activeInHierarchy, "(Scenario 4) UI should close after conversation ends.");
        
        // Unsubscribe and cleanup
        realUI.OnTextComplete -= OnTextCompleteHandler;
        realUI.OnConversationEnd -= OnConversationEndHandler;
        realUI.OnOpened -= OnOpenedHandler;
        realUI.OnClosed -= OnClosedHandler;
        realUI.OnResponseChosen -= OnResponseChosenHandler;
        
        if (_controller is not null) DestroyImmediate(_controller.gameObject);
        
        InfoDone("DialogController_PlaymodeSelfTest: PASSED (Scenario 4)");
    }


    private void BuildSystem()
    {
        var rootGo = new GameObject("DialogSystem_TestRoot");
        rootGo.SetActive(false);

        _nodeManager = rootGo.AddComponent<NodeManager>();
        // _nodeManager = NodeManager.Instance;

        _controller = rootGo.AddComponent<DialogController>();
        _ui = new GameObject("TestDialogUI").AddComponent<TestDialogUIManager>();

        _controller.NodeManager = _nodeManager;
        _controller.UiManager = _ui;
        
        rootGo.SetActive(true);
    }

    private void BuildGraph_ABC()
    {
        _nodeC = MakeNode("C", "Goodbye", new List<DialogNode>(), new List<Response>());

        _nodeB = MakeNode("B", "No choices here, continue...", new List<DialogNode>(){_nodeC}, new List<Response>());

        var toB = MakeResponse("Go to B", _nodeB);
        _nodeA = MakeNode("A", "Hello!", null, new List<Response> { toB });
    }

    private void BuildGraph_OnlyAWithChoice()
    {
        var toEnd = MakeResponse("Finish", null);
        _nodeA = MakeNode("A", "Choose to end or cancel", new List<DialogNode>(), new List<Response> { toEnd });
    }

    private static DialogNode MakeNode(string id, string text, List<DialogNode> nextNodes, List<Response> responses)
    {
        var node = ScriptableObject.CreateInstance<DialogNode>();
        node.Id = id;
        node.DialogText = text;
        node.NextNodes = nextNodes ?? new List<DialogNode>();
        node.Responses = responses ?? new List<Response>();
        node.Conditions = new List<Condition>();
        node.FlagsToChange = new List<FlagChange>();

        var character = ScriptableObject.CreateInstance<CharacterData>();
        character.Id = "npc_" + id;
        character.Name = "NPC " + id;
        node.Character = character;

        return node;
    }

    private static Response MakeResponse(string text, DialogNode next = null)
    {
        return new Response
        {
            Text = text,
            NextNode = next,
            Conditions = new List<Condition>(),
            FlagsToChange = new List<FlagChange>(),
        };
    }

    private void Cleanup()
    {
        if (_controller != null) DestroyImmediate(_controller.gameObject);
        if (_ui != null) DestroyImmediate(_ui.gameObject);
    }
}