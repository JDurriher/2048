using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Block : MonoBehaviour
{
    public int Value;
    public Node Node;
    public Block MergingBlock;
    public bool Merging;
    public Vector2 Pos => transform.position;
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private TextMeshPro text;

    public void Init(BlockType type)
    {
        Value = type.Value;
        _renderer.color = type.Color;
        text.text = type.Value.ToString();
    }

    public void SetBlock(Node node)
    {
        if (Node != null) Node.OccupiedBlock = null;
        Node = node;
        Node.OccupiedBlock = this;
    }

    public void MergeBlock(Block blockToMergeWith)
    {
        // Set the block we are merging with
        MergingBlock = blockToMergeWith;

        // Set the current node as unoccupied to allow other bllocks to use it
        Node.OccupiedBlock = null;

        // Set the base block as merging so it does not get used twice
        blockToMergeWith.Merging = true;
    }

    public bool CanMerge(int value) => value == Value && !Merging && MergingBlock == null;

}
