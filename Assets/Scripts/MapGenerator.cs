using UnityEngine;
using System.Collections;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour {

	public enum DrawMode {NoiseMap, ColourMap, Mesh};
	public DrawMode drawMode;

	public const int mapChunkSize = 241;
	[Range(0,6)]
	public int Resolution;
	public float noiseScale;

	public int octaves;
	[Range(0,1)]
	public float persistance;
	public float lacunarity;

	public int seed;
	public Vector2 offset;

	public bool autoUpdate;

	public float meshHeightMultiplier;

	public AnimationCurve meshHeightCurve;
	public TerrainType[] regions;

	public void RequestMapData(Action<MapData> callback){

	}

	public void MapDataThreads(Action<MapData> callback){

	}
	public void Update(){
		
	}
	public void DrawMapInEditor(){
		MapData mapData= GenerateMapData();
		MapDisplay display = FindObjectOfType<MapDisplay> ();
		if (drawMode == DrawMode.NoiseMap) {
			display.DrawTexture (TextureGenerator.TextureFromHeightMap (mapData.heightMap));
		} else if (drawMode == DrawMode.ColourMap) {
			display.DrawTexture (TextureGenerator.TextureFromColourMap (mapData.colorMap, mapChunkSize, mapChunkSize));
		} else if (drawMode == DrawMode.Mesh) {
			display.DrawMesh (MeshGenerator.GenerateTerrainMesh (mapData.heightMap,meshHeightMultiplier,meshHeightCurve,Resolution), TextureGenerator.TextureFromColourMap (mapData.colorMap, mapChunkSize, mapChunkSize));
		}
	}

	MapData GenerateMapData() {
		float[,] noiseMap = Noise.GenerateNoiseMap (mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, offset);

		Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
		for (int y = 0; y < mapChunkSize; y++) {
			for (int x = 0; x < mapChunkSize; x++) {
				float currentHeight = noiseMap [x, y];
				for (int i = 0; i < regions.Length; i++) {
					if (currentHeight <= regions [i].height) {
						colourMap [y * mapChunkSize + x] = regions [i].colour;
						break;
					}
				}
			}
		}

	return new MapData(noiseMap,colourMap);
	}

	void OnValidate() {
		if (lacunarity < 1) {
			lacunarity = 1;
		}
		if (octaves < 0) {
			octaves = 0;
		}
	}
}

[System.Serializable]
public struct TerrainType {
	public string name;
	public float height;
	public Color colour;
}

public struct MapData{
	public float[,] heightMap;
	public Color[] colorMap;

	public MapData(float[,] heightMap,Color[] colorMap){
		this.heightMap=heightMap;
		this.colorMap=colorMap;
	}
}