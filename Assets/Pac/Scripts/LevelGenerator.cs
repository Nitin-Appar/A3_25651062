using System;
using System.Collections.Generic;
using UnityEngine;


public sealed class LevelGenerator : MonoBehaviour
{
    [SerializeField]
    private GameObject manualLevel;

    [SerializeField]
    private Camera levelCamera;


    [SerializeField]
    private GameObject[] tilePrefabs = new GameObject[9];

    [SerializeField, Min(0f)]
    private float cameraPadding = 1f;

    private int[,] levelMap =
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
        {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
        {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
        {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
        {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
        {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
        {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
        {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
        {0,0,0,0,0,2,5,4,4,0,3,4,4,8},
        {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
        {0,0,0,0,0,0,5,0,0,0,4,0,0,0}
    };


    private static readonly int[,] Ports =
    {
        {0,0,0,0},
        {0,1,1,0},
        {0,1,0,1},
        {0,2,2,0},
        {0,2,0,2},
        {0,0,0,0},
        {0,0,0,0},
        {0,1,2,1},
        {0,2,0,2}
    };

    private static readonly int[] DR = { -1, 0, 1, 0 };
    private static readonly int[] DC = { 0, 1, 0, -1 };

    private int[,] fullMap;
    private int rows;
    private int columns;
    private int searchNodes;
    private int lastWidth;
    private int lastHeight;
    private GameObject generatedLevel;

    private void Start()
    {
        try
        {
            ValidateReferences();
            MirrorArray();

            List<int>[] possibilities = MakeDomains();
            searchNodes = 0;

            if (!Solve(ref possibilities))
            {
                throw new InvalidOperationException(
                    "no wall"
                );
            }

            manualLevel.SetActive(false);
            Destroy(manualLevel);

            generatedLevel = new GameObject("Level01_Generated");

            for (int row = 0; row < rows; row++)
            {
                Transform rowParent =
                    new GameObject($"Row_{row:00}").transform;

                rowParent.SetParent(generatedLevel.transform, false);

                for (int col = 0; col < columns; col++)
                {
                    int tile = fullMap[row, col];

                    if (tile == 0)
                    {
                        continue;
                    }

                    GameObject piece = Instantiate(
                        tilePrefabs[tile],
                        rowParent
                    );

                    piece.name = $"R{row:00}_C{col:00}_T{tile}";

                    piece.transform.localPosition =
                        new Vector3(col, -row, 0f);

                    piece.transform.localRotation =
                        Quaternion.Euler(
                            0f,
                            0f,
                            -90f * possibilities[
                                row * columns + col
                            ][0]
                        );

                    piece.transform.localScale = Vector3.one;
                }
            }

            FitCamera();

            Debug.Log(
                $"Generated {columns} x {rows} cells; " +
                $"rotation search nodes: {searchNodes}.",
                this
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "LevelGenerator: " + exception.Message,
                this
            );

            enabled = false;
        }
    }

    private void ValidateReferences()
    {
        if (manualLevel == null || levelCamera == null)
        {
            throw new InvalidOperationException(
                "Assign Manual Level and Level Camera."
            );
        }

        if (tilePrefabs == null || tilePrefabs.Length != 9)
        {
            throw new InvalidOperationException(
                "Tile Prefabs Size must be 9, with index 0 empty."
            );
        }

        for (int i = 1; i <= 8; i++)
        {
            if (tilePrefabs[i] == null)
            {
                throw new InvalidOperationException(
                    $"Assign Tile Prefabs Element {i}."
                );
            }
        }

        if (levelMap.GetLength(0) < 1 ||
            levelMap.GetLength(1) < 1 ||
            levelMap[0, 0] != 1)
        {
            throw new InvalidOperationException(
                "Map must be non-empty and start with tile 1."
            );
        }

        foreach (int value in levelMap)
        {
            if (value < 0 || value > 8)
            {
                throw new InvalidOperationException(
                    "Map values must be between 0 and 8."
                );
            }
        }
    }

    private void MirrorArray()
    {
        int halfRows = levelMap.GetLength(0);
        int halfColumns = levelMap.GetLength(1);

        rows = 2 * halfRows - 1;
        columns = 2 * halfColumns;

        fullMap = new int[rows, columns];

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                int sourceRow =
                    row < halfRows
                        ? row
                        : rows - 1 - row;

                int sourceCol =
                    col < halfColumns
                        ? col
                        : columns - 1 - col;

                fullMap[row, col] =
                    levelMap[sourceRow, sourceCol];
            }
        }
    }

    private List<int>[] MakeDomains()
    {
        var domains = new List<int>[rows * columns];

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                int tile = fullMap[row, col];

                int count =
                    tile == 0 || tile == 5 || tile == 6
                        ? 1
                        : tile == 2 || tile == 4 || tile == 8
                            ? 2
                            : 4;

                var rotations = new List<int>();

                for (int rotation = 0;
                     rotation < count;
                     rotation++)
                {
                    rotations.Add(rotation);
                }

                domains[row * columns + col] = rotations;
            }
        }

        domains[0] = new List<int> { 0 };

        return domains;
    }

    private static int Port(
        int tile,
        int clockwiseTurns,
        int direction)
    {
        return Ports[
            tile,
            (direction - clockwiseTurns + 4) % 4
        ];
    }

    private bool Propagate(List<int>[] domains)
    {
        bool changed;

        do
        {
            changed = false;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    List<int> options =
                        domains[row * columns + col];

                    int tile = fullMap[row, col];

                    for (int index = options.Count - 1;
                         index >= 0;
                         index--)
                    {
                        int rotation = options[index];
                        bool valid = true;

                        for (int direction = 0;
                             direction < 4 && valid;
                             direction++)
                        {
                            int port =
                                Port(tile, rotation, direction);

                            int neighbourRow =
                                row + DR[direction];

                            int neighbourColumn =
                                col + DC[direction];

                            if (neighbourRow < 0 ||
                                neighbourRow >= rows ||
                                neighbourColumn < 0 ||
                                neighbourColumn >= columns)
                            {
                                bool openSide =
                                    (direction == 1 ||
                                     direction == 3) &&
                                    (tile == 2 ||
                                     tile == 4 ||
                                     tile == 8);

                                valid = port == 0 || openSide;
                                continue;
                            }

                            bool supported = false;

                            foreach (
                                int neighbourRotation in
                                domains[
                                    neighbourRow * columns +
                                    neighbourColumn
                                ])
                            {
                                int neighbourPort = Port(
                                    fullMap[
                                        neighbourRow,
                                        neighbourColumn
                                    ],
                                    neighbourRotation,
                                    (direction + 2) % 4
                                );

                                if (port == neighbourPort)
                                {
                                    supported = true;
                                    break;
                                }
                            }

                            valid = supported;
                        }

                        if (!valid)
                        {
                            options.RemoveAt(index);
                            changed = true;
                        }
                    }

                    if (options.Count == 0)
                    {
                        return false;
                    }
                }
            }
        }
        while (changed);

        return true;
    }

    private bool Solve(ref List<int>[] domains)
    {
        searchNodes++;

        if (searchNodes > 100000)
        {
            throw new InvalidOperationException(
                "Rotation search exceeded 100000 choices; " +
                "inspect map connections."
            );
        }

        if (!Propagate(domains))
        {
            return false;
        }

        int chosen = -1;

        for (int i = 0; i < domains.Length; i++)
        {
            if (domains[i].Count > 1 &&
                (chosen < 0 ||
                 domains[i].Count < domains[chosen].Count))
            {
                chosen = i;
            }
        }

        if (chosen < 0)
        {
            return true;
        }

        foreach (int rotation in domains[chosen])
        {
            var trial = new List<int>[domains.Length];

            for (int i = 0; i < domains.Length; i++)
            {
                trial[i] = new List<int>(domains[i]);
            }

            trial[chosen] = new List<int> { rotation };

            if (Solve(ref trial))
            {
                domains = trial;
                return true;
            }
        }

        return false;
    }

    private void FitCamera()
    {
        levelCamera.orthographic = true;

        levelCamera.transform.position = new Vector3(
            (columns - 1) * 0.5f,
            -(rows - 1) * 0.5f,
            -10f
        );

        levelCamera.transform.rotation = Quaternion.identity;

        float aspect = Mathf.Max(
            0.01f,
            levelCamera.aspect
        );

        levelCamera.orthographicSize = Mathf.Max(
            rows * 0.5f + cameraPadding,
            (columns * 0.5f + cameraPadding) / aspect
        );

        lastWidth = levelCamera.pixelWidth;
        lastHeight = levelCamera.pixelHeight;
    }

    private void LateUpdate()
    {
        if (generatedLevel != null &&
            levelCamera != null &&
            (levelCamera.pixelWidth != lastWidth ||
             levelCamera.pixelHeight != lastHeight))
        {
            FitCamera();
        }
    }
}