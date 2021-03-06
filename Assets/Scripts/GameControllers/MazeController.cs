using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MazeController : MonoBehaviour {
    [Range(1, 100)]
    public int width = 10;
    [Range(1, 100)]
    public int height = 10;

    public HUD hud = null;

    public GameObject floor = null;
    public GameObject player = null;
    public GameObject target = null;

    public GameObject wallPrefab = null;
    public GameObject tokenPrefab = null;
    
    public Maze maze = null;
    public Grid grid = null;

    [Range(0f, 5.0f)]
    public float tokenHeight = 1.0f;
    [Range(1.0f, 5.0f)]
    public float wallWidth = 1.42f;
    [Range(0.0f, 5.0f)]
    public float wallHeight = 3.0f;
    [Range(0f, 1.0f)]
    public float wallDepth = 0.05f;
    [Range(0f, 100f)]
    public float floorTextureScale = 4.0f;

    private PlayerController playerController = null;
    private Material floorMaterial;
    private MazeGenerator generator = null;

    //public MazeAudioSourceController MazeAudio;

    void Start() {
        //Debug.Log("audio output -> " + GlobalGameSettings.audioOutputEnabled + ", voice control -> " + GlobalGameSettings.voiceControlEnabled);

        playerController = player.GetComponent<PlayerController>();
        floorMaterial = floor.GetComponent<Renderer>().sharedMaterial;

        // setup the grid
        grid = GetComponent<Grid>();
        grid.cellSize = new Vector3(wallWidth, wallWidth, 1.0f);
        grid.cellGap = Vector3.zero;

        wallPrefab.transform.localScale = new Vector3(wallWidth, wallHeight, wallDepth);

        //MazeAudio = GetComponent<MazeAudioSourceController>();

        // TODO: move to a higher level game controller
        maze = ScriptableObject.CreateInstance<Maze>();
        maze.Init(new Vector2Int(width, height));

        generator = new MazeGenerator();
        generator.Generate(ref maze);

        // StartCoroutine(RenderNextFrame());
        Render();

        // start playing the target sound
        if (GlobalGameSettings.audioOutputEnabled) {
            target.GetComponentInChildren<AudioSource>().Play();
        }

        // start the HUD
        hud.StartGame();
    }

    private IEnumerator RenderNextFrame() {
        yield return new WaitForEndOfFrame();
        Render();
    }

    private Vector3 CellToLocalScale(Vector2 scale) {
        return grid.CellToLocalInterpolated(new Vector3(scale.x, scale.y, 1.0f));
    }

    void Clear() {
        // remove all dynamic objects
        foreach (Transform child in transform) {
            var obj = child.gameObject;
            if (obj.CompareTag("Wall") || obj.CompareTag("Token")) {
                GameObject.Destroy(obj);
            }
        }
    }

    void Render() {
        Clear();
        if (maze == null) return;

        // place the target
        target.transform.localPosition = grid.GetCellCenterLocal((Vector3Int) maze.size - Vector3Int.one) +
            Vector3.up * tokenHeight;

        // place the player
        player.transform.localPosition = grid.GetCellCenterLocal(Vector3Int.zero);
        playerController.LookAt(target.transform.position);

        // resize the floor
        floor.transform.localScale = CellToLocalScale(maze.size);
        floorMaterial.mainTextureScale = (Vector2) maze.size * floorTextureScale;

        // create the walls
        for (int x = 0; x < maze.size.x; x++) {
            for (int y = 0; y < maze.size.y; y++) {
                Maze.WallState wallState = maze.walls[x, y];
                Vector2Int position = new Vector2Int(x, y);

                if (wallState.HasFlag(Maze.WallState.UP)) {
                    InstantiateWall(position + new Vector2(0.5f, 1.0f), true);
                }

                if (wallState.HasFlag(Maze.WallState.RIGHT)) {
                    InstantiateWall(position + new Vector2(1.0f, 0.5f), false);
                }

                if (wallState.HasFlag(Maze.WallState.DOWN) && y == 0) {
                    InstantiateWall(position + new Vector2(0.5f, 0.0f), true);
                }

                if (wallState.HasFlag(Maze.WallState.LEFT) && x == 0) {
                    InstantiateWall(position + new Vector2(0, 0.5f), false);
                }
            }
        }

        // create the tokens
        foreach (var token in maze.tokens) {
            InstantiateToken(token + new Vector2(0.5f, 0.5f));
        }
    }

    private void InstantiateWall(Vector2 gridPosition, bool horizontal) {
        var wall = Instantiate(wallPrefab,
                grid.CellToLocalInterpolated(gridPosition) + Vector3.up * wallPrefab.transform.position.y,
                horizontal ? Quaternion.identity : Quaternion.AngleAxis(90, Vector3.up),
                transform);
    }

    private void InstantiateToken(Vector2 gridPosition) {
        Instantiate(tokenPrefab,
                grid.CellToLocalInterpolated(gridPosition) + Vector3.up * tokenHeight,
                Quaternion.identity,
                transform);
    }
}
