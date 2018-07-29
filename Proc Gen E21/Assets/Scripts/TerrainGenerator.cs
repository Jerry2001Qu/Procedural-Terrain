using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TerrainGenerator : MonoBehaviour {

	const float viewerMoveThresholdForChunkUpdate = 25f;
	const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;


	public int colliderLODIndex;
	public LODInfo[] detailLevels;

	public MeshSettings meshSettings;
	public HeightMapSettings heightMapSettings;
	public TextureData textureSettings;

	public Transform viewer;
	public Material mapMaterial;

	public GameObject capsule;

	Vector2 viewerPosition;
	Vector2 viewerPositionOld;

	float meshWorldSize;
	int chunksVisibleInViewDst;

	Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

	void Start() {

		textureSettings.ApplyToMaterial (mapMaterial);
		textureSettings.UpdateMeshHeights (mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

		float maxViewDst = detailLevels [detailLevels.Length - 1].visibleDstThreshold;
		meshWorldSize = meshSettings.meshWorldSize;
		chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize);

		UpdateVisibleChunks ();
	}

	void Update() {
		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z);

		if (viewerPosition != viewerPositionOld) {
			foreach (TerrainChunk chunk in visibleTerrainChunks) {
				chunk.UpdateCollisionMesh ();
			}
		}

		if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
			viewerPositionOld = viewerPosition;
			UpdateVisibleChunks ();
		}

		if (Input.GetKeyDown ("q")) {

			Vector2 closestChunk = closest ();

			terrainChunkDictionary [closestChunk].jMultiplier = (float)(terrainChunkDictionary [closestChunk].jMultiplier + 8f);

			TerrainChunk newChunk = new TerrainChunk (closestChunk,heightMapSettings,meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial);
			newChunk.jMultiplier = terrainChunkDictionary [closestChunk].jMultiplier;
			terrainChunkDictionary [closestChunk].DestroyObj ();
			terrainChunkDictionary.Remove (closestChunk);
			terrainChunkDictionary.Add (closestChunk, newChunk);
			newChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;
			newChunk.Load ();

			/*
			foreach (Vector2 s in terrainChunkDictionary.Keys.ToList()) {
				Debug.Log (terrainChunkDictionary[s].jMultiplier);
				terrainChunkDictionary[s].jMultiplier = (float)(terrainChunkDictionary[s].jMultiplier + 8f);

				TerrainChunk newChunk = new TerrainChunk (s,heightMapSettings,meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial);
				newChunk.jMultiplier = terrainChunkDictionary[s].jMultiplier;
				terrainChunkDictionary [s].DestroyObj ();
				terrainChunkDictionary.Remove (s);
				terrainChunkDictionary.Add (s, newChunk);
				newChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;
				newChunk.Load ();
			} */
		}

		if (Input.GetKeyDown ("e")) {

			Vector2 closestChunk = closest ();

			terrainChunkDictionary [closestChunk].jMultiplier = (float)(terrainChunkDictionary [closestChunk].jMultiplier - 8f);

			TerrainChunk newChunk = new TerrainChunk (closestChunk,heightMapSettings,meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial);
			newChunk.jMultiplier = terrainChunkDictionary [closestChunk].jMultiplier;
			terrainChunkDictionary [closestChunk].DestroyObj ();
			terrainChunkDictionary.Remove (closestChunk);
			terrainChunkDictionary.Add (closestChunk, newChunk);
			newChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;
			newChunk.Load ();

			/*
			foreach (Vector2 s in terrainChunkDictionary.Keys.ToList()) {
				Debug.Log (terrainChunkDictionary[s].jMultiplier);
				terrainChunkDictionary[s].jMultiplier = (float)(terrainChunkDictionary[s].jMultiplier - 8f);

				TerrainChunk newChunk = new TerrainChunk (s,heightMapSettings,meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial);
				newChunk.jMultiplier = terrainChunkDictionary[s].jMultiplier;
				terrainChunkDictionary [s].DestroyObj ();
				terrainChunkDictionary.Remove (s);
				terrainChunkDictionary.Add (s, newChunk);
				newChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;
				newChunk.Load ();
			} */
		}
	}

	public Vector2 closest() {
		float distance = 0f, temp = 0f;
		Vector2 closestChunk = new Vector2(0, 0);
		bool first = true;
		foreach (Vector2 s in terrainChunkDictionary.Keys.ToList()) {
			if (first) {
				first = false;
				distance = terrainChunkDictionary [s].distanceTo (capsule);
				closestChunk = s;
			}
			temp = terrainChunkDictionary [s].distanceTo (capsule);
			if (temp < distance) {
				distance = temp;
				closestChunk = s;
			}
		}

		return closestChunk;
	}

	void UpdateVisibleChunks() {
		HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2> ();
		for (int i = visibleTerrainChunks.Count-1; i >= 0; i--) {
			alreadyUpdatedChunkCoords.Add (visibleTerrainChunks [i].coord);
			visibleTerrainChunks [i].UpdateTerrainChunk ();
		}
			
		int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / meshWorldSize);
		int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / meshWorldSize);

		for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
			for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
				Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
				if (!alreadyUpdatedChunkCoords.Contains (viewedChunkCoord)) {
					if (terrainChunkDictionary.ContainsKey (viewedChunkCoord)) {
						terrainChunkDictionary [viewedChunkCoord].UpdateTerrainChunk ();
					} else {
						TerrainChunk newChunk = new TerrainChunk (viewedChunkCoord,heightMapSettings,meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial);
						terrainChunkDictionary.Add (viewedChunkCoord, newChunk);
						newChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;
						newChunk.Load ();
					}
				}

			}
		}
	}

	void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible) {
		if (isVisible) {
			visibleTerrainChunks.Add (chunk);
		} else {
			visibleTerrainChunks.Remove (chunk);
		}
	}

}

[System.Serializable]
public struct LODInfo {
	[Range(0,MeshSettings.numSupportedLODs-1)]
	public int lod;
	public float visibleDstThreshold;


	public float sqrVisibleDstThreshold {
		get {
			return visibleDstThreshold * visibleDstThreshold;
		}
	}
}
