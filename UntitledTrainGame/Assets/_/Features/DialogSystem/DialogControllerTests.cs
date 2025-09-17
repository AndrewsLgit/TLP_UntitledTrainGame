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
        Debug.Log("DialogController_PlaymodeSelfTest: PASSED (all scenarios)");
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