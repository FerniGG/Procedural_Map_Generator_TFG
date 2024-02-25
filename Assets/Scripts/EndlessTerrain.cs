using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class EndlessTerrain : MonoBehaviour
{
    const float viewerMoveThresholForChunkUpdate=25f;
    const float sqrViewerMoveThresholForChunkUpdate = viewerMoveThresholForChunkUpdate *viewerMoveThresholForChunkUpdate ;
    public LODInfo[] detailLevels;
    public static float maxViewDst;
    public Transform viewer;
    public Material mapMaterial;
    static MapGenerator mapGenerator;
    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    int chunkSize;
    int chunksVisibleInViewDst;

    Dictionary<Vector2,TerrainChunk>terrainChunkDictionary=new Dictionary<Vector2,TerrainChunk>();
    List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>(); 
    
    void Start(){
        mapGenerator = FindAnyObjectByType<MapGenerator>();
        maxViewDst = detailLevels[detailLevels.Length-1].visibleDstThreshold;
        chunkSize = MapGenerator.mapChunkSize -1;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);
        
        UpdateVisibleChunks();

    }

    void Update(){
        viewerPosition = new Vector2(viewer.position.x,viewer.position.z);

        if((viewerPositionOld-viewerPosition).sqrMagnitude > sqrViewerMoveThresholForChunkUpdate){
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }
    void UpdateVisibleChunks(){

        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);
        
        for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset,currentChunkCoordY + yOffset);
                if(terrainChunkDictionary.ContainsKey(viewedChunkCoord)){
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    if(terrainChunkDictionary[viewedChunkCoord].IsVisible()){
                        terrainChunksVisibleLastUpdate.Add(terrainChunkDictionary[viewedChunkCoord]);
                    }
                }else{
                    terrainChunkDictionary.Add(viewedChunkCoord,new TerrainChunk(viewedChunkCoord,chunkSize,detailLevels,transform,mapMaterial));
                }
            }
            
        }
    }

    public class TerrainChunk{
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;
        MapData mapData;
        bool mapDataReceived;
        int previousResolutionIndex = -1;
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        LODInfo[] detailLevels;
        LODMesh[] detailMeshes;
        public TerrainChunk(Vector2 coord, int size,LODInfo[] detailLevels ,Transform parent,Material material){
            this.detailLevels = detailLevels;
            position=coord *size;
            Vector3 positionV3 = new Vector3(position.x,0,position.y);
            bounds = new Bounds(position,Vector2.one *size);
            meshObject =new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();

            meshRenderer.material=material;

            meshObject.transform.position=positionV3;
            //meshObject.transform.localScale = Vector3.one * size / 10f;
            meshObject.transform.parent = parent;
            SetVisible(false);

            detailMeshes = new LODMesh[detailLevels.Length];
            for (var i = 0; i < detailLevels.Length; i++)
            {
                detailMeshes[i] = new LODMesh(detailLevels[i].resolution, UpdateTerrainChunk); 
            }

            mapGenerator.RequestMapData(position , OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData){
            this.mapData = mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colourMap,MapGenerator.mapChunkSize,MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;
            UpdateTerrainChunk();

           // mapGenerator.RequestMeshData(mapData,OnMeshDataReceived);
        }

         /* void OnMeshDataReceived(MeshData meshData){
            meshFilter.mesh = meshData.CreateMesh();
        }*/

        public void UpdateTerrainChunk(){
            if(mapDataReceived){
          float viewerDstFromNearestEdge =  Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
          bool visible = viewerDstFromNearestEdge <= maxViewDst;

          if(visible){
            int resolutionIndex =0;
            for (var i = 0; i < detailLevels.Length-1; i++)
            {
                if(viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold){
                    resolutionIndex= i+1;
                }else{
                    break;
                }
            }if(resolutionIndex!=previousResolutionIndex){
                LODMesh resolutionMesh =detailMeshes[resolutionIndex];
                if(resolutionMesh.hasMesh){
                    previousResolutionIndex = resolutionIndex;
                    meshFilter.mesh = resolutionMesh.mesh;
                }else if(!resolutionMesh.hasRequestedMesh){
                    resolutionMesh.RequestMesh(mapData);
                }
            }
          }
          SetVisible(visible);
            }
        }

        public void SetVisible(bool visible){
            meshObject.SetActive(visible);
        }

        public bool IsVisible(){
            return meshObject.activeSelf;
        }
    }
    //Esta clase se usa para renderizar con menor resolucion los chunks mas alejados, y con mayor las que estan cerca
    class LODMesh{
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        public int resolution;
        System.Action updateCallback;
        public LODMesh(int resolucion, System.Action updateCallback){
            this.resolution = resolucion;
            this.updateCallback = updateCallback;
        }
        public void OnMeshDataReceived(MeshData meshData){
            mesh = meshData.CreateMesh();
            hasMesh = true;
        }
        public void RequestMesh(MapData mapData){
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData,resolution,OnMeshDataReceived);
        }
    }
    [System.Serializable]
    public struct LODInfo{
        public int resolution;
        public float visibleDstThreshold;
    }
}
