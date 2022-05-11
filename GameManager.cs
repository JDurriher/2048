using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int width = 4;
    [SerializeField] private int height = 4;
    [SerializeField] private Node nodePrefab;
    [SerializeField] private Block blockPrefab;
    [SerializeField] private SpriteRenderer boardPrefab;
    [SerializeField] private List<BlockType> types;
    [SerializeField] private float travelTime = 0.2f;
    [SerializeField] private int windCondition = 2048;

    [SerializeField] private GameObject winScreen, loseScreen;

    private List<Node> nodes;       // For storing nodes we spawn
    private List<Block> blocks;
    private GameState state;
    private int round;

    private BlockType GetBlockTypeByValue(int value) => types.First(t => t.Value == value);

    void Start()
    {
        ChangeState(GameState.GenerateLevel);
    }

    private void ChangeState(GameState newstate)
    {
        state = newstate;

        switch (newstate)
        {
            case GameState.GenerateLevel:
                GenerateGrid();
                break;
            case GameState.SpawningBlocks:
                SpawnBlocks(round++ == 0 ? 2 : 1);
                break;
            case GameState.WaitingInput:
                break;
            case GameState.Moving:
                break;
            case GameState.Win:
                winScreen.SetActive(true);
                break;
            case GameState.Lose:
                loseScreen.SetActive(true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newstate), newstate, null);
        }
    }

    void Update()
    {
        if (state != GameState.WaitingInput) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow)) Shift(Vector2.left);
        if (Input.GetKeyDown(KeyCode.RightArrow)) Shift(Vector2.right);
        if (Input.GetKeyDown(KeyCode.UpArrow)) Shift(Vector2.up);
        if (Input.GetKeyDown(KeyCode.DownArrow)) Shift(Vector2.down);
    }

    void GenerateGrid()
    {
        round = 0;
        nodes = new List<Node>();       // Initiate list
        blocks = new List<Block>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var node = Instantiate(nodePrefab, new Vector2(x, y), Quaternion.identity);
                nodes.Add(node);
            }
        }

        var center = new Vector2((float)width / 2 - 0.5f, (float)height / 2 - 0.5f);

        var board = Instantiate(boardPrefab, center, Quaternion.identity);
        board.size = new Vector2(width, height);

        Camera.main.transform.position = new Vector3(center.x, center.y, -10);

        ChangeState(GameState.SpawningBlocks);
    }

    void SpawnBlocks(int amount)
    {
        var freeNodes = nodes.Where(n => n.OccupiedBlock == null).OrderBy(b => Random.value).ToList();

        foreach (var node in freeNodes.Take(amount))
        {
            SpawnBlock(node, Random.value > 0.8f ? 4 : 2);
        }

        if (freeNodes.Count() == 1)
        {
            ChangeState(GameState.Lose);
            return;
        }

        ChangeState(blocks.Any(b => b.Value == windCondition) ? GameState.Win : GameState.WaitingInput);
    }

    void SpawnBlock(Node node, int value)
    {
        var block = Instantiate(blockPrefab, node.Pos, Quaternion.identity);
        block.Init(GetBlockTypeByValue(value));
        block.SetBlock(node);
        blocks.Add(block);
    }

    void Shift(Vector2 dir)
    {
        ChangeState(GameState.Moving);

        var orderedBlocks = blocks.OrderBy(b => b.Pos.x).ThenBy(b => b.Pos.y).ToList();
        if (dir == Vector2.right || dir == Vector2.up) orderedBlocks.Reverse();

        foreach (var block in orderedBlocks)
        {
            var next = block.Node;      // Start with current node
            do
            {
                block.SetBlock(next);   // Set current node immediately

                var possibleNode = GetNodeAtPosition(next.Pos + dir);   // Check if another node next to me
                if (possibleNode != null)
                {
                    // We know a node is present
                    // If it's possible to merge, set merge
                    if (possibleNode.OccupiedBlock != null && possibleNode.OccupiedBlock.CanMerge(block.Value))
                    {
                        block.MergeBlock(possibleNode.OccupiedBlock);
                    }
                    // Otherwise, can we move to this spot?
                    else if (possibleNode.OccupiedBlock == null) next = possibleNode;    // If is available set next var to this new block just checked is available
                }
            } while (next != block.Node); // Condition to see if we should go back to top and set this block to our furthest block
        }

        var sequence = DOTween.Sequence();

        foreach (var block in orderedBlocks)
        {
            var movePoint = block.MergingBlock != null ? block.MergingBlock.Node.Pos : block.Node.Pos;

            sequence.Insert(0, block.transform.DOMove(movePoint, travelTime));
        }

        sequence.OnComplete(() =>
        {
            foreach (var block in orderedBlocks.Where(b => b.MergingBlock != null))
            {
                MergeBlocks(block.MergingBlock, block);
            }

            ChangeState(GameState.SpawningBlocks);
        });

    }

    void MergeBlocks(Block baseBlock, Block mergingBlock)
    {
        SpawnBlock(baseBlock.Node, baseBlock.Value * 2);

        RemoveBlock(baseBlock);
        RemoveBlock(mergingBlock);
    }

    void RemoveBlock(Block block)
    {
        blocks.Remove(block);
        Destroy(block.gameObject);
    }

    Node GetNodeAtPosition(Vector2 pos)
    {
        return nodes.FirstOrDefault(n => n.Pos == pos);
    }

}

[Serializable]

public struct BlockType
{
    public int Value;
    public Color Color;
}

public enum GameState
{
    GenerateLevel,
    SpawningBlocks,
    WaitingInput,
    Moving,
    Win,
    Lose
}