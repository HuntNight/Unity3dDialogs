using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
{
    DialogGraph _graph;

    public void Configure(DialogGraph graph)
    {
        _graph = graph;
    }

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        var tree = new List<SearchTreeEntry>
             {
                 new SearchTreeGroupEntry(new GUIContent("Create")),
                 new SearchTreeEntry(new GUIContent("Node")) {
                     level = 1
                 }
             };

        return tree;
    }

    public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
    {
        _graph.CreateNode(context.screenMousePosition);
        return true;
    }
}