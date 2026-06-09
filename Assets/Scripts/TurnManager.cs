using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Mathematics;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TurnManager : MonoBehaviour
{
    Coroutine fadeRoutine;

    public Rock rock;
    public ScoringZone scoringZone;

    public TMP_Text teamAText;
    public TMP_Text teamBText;
    public TMP_Text turnText;
    public TMP_Text roundEndText;
    public GameObject endGamePanel;
    public GameObject controlPanel;
    public TMP_Text winnerText;
    public TMP_Text finalScoreText;
    public Button restartButton;
    public TMP_Text hazardText1;
    public TMP_Text hazardText2;
    public TMP_Text hazardText3;

    private Coroutine hazardFadeRoutine;

    public Material frozenLakeSkybox;
    public Material volcanoSkybox;
    public Material cloudSkybox;

    public GameObject frozenLakeMap;
    public GameObject volcanoMap;
    public GameObject cloudMap;

    public Renderer iceRenderer;
    public Renderer outerSeparatorRenderer;
    public Renderer innerSeparatorRenderer;

    public Material iceMaterial1;
    public Material iceMaterial2;
    public Material iceMaterial3;

    public PhysicsMaterial lakePhysics;
    public PhysicsMaterial obsidianPhysics;
    public PhysicsMaterial cloudPhysics;

    public Collider iceCollider;

    private List<GameObject> spawnedRocks = new List<GameObject>();

    public Transform spawnPoint;

    private int teamAStones = 0;
    private int teamBStones = 0;

    private int teamAScore = 0;
    private int teamBScore = 0;
    public int maxStonesPerTeam = 4;

    public int currentRound = 1;
    public int maxRounds = 3;
    public TMP_Text roundText;

    private Team currentTeam = Team.TeamA;

    public Transform InnerSeparator;

    public GameObject sun;
    public Light sunlight;
    public Volume globalVolume;
    private Bloom bloom;
    private ColorAdjustments adjust;
    private SplitToning split;
    private WhiteBalance whiteB;


    void Start()
    {
        rock.isRoundEnding = true;

        globalVolume.profile.TryGet(out bloom);
        globalVolume.profile.TryGet(out split);
        globalVolume.profile.TryGet(out whiteB);
        globalVolume.profile.TryGet(out adjust);
        
        StartGameFromMenu();
    }

    public void StartGameFromMenu()
    {
        // enable player input and hide menu ui
        rock.inputEnabled = true;

        ShowRoundHazardText();

        if (controlPanel != null)
            controlPanel.SetActive(false);

        // reset game state for a fresh match
        rock.isRoundEnding = false;
        currentRound = 1;
        SetRoundEnvironment();
        currentTeam = Team.TeamA;
        sunlight = sun.GetComponent<Light>();
        
        UpdateRoundUI();
        UpdateScoreUI();

        StartTurn();
    }

    void SetRoundEnvironment()
    {
        frozenLakeMap.SetActive(false);
        volcanoMap.SetActive(false);
        cloudMap.SetActive(false);

        if (currentRound == 1)
        {
            frozenLakeMap.SetActive(true);
            RenderSettings.skybox = frozenLakeSkybox;
            ApplyIceMaterial(iceMaterial1);
            iceCollider.material = lakePhysics;
            rock.SetIceEffectColor(Color.white);
            rock.fragileIceEnabled = true;
            rock.lavaEnabled = false;

            sun.transform.rotation = Quaternion.Euler(90, 0, 0);
            split.balance.value = -16f;
            whiteB.temperature.value = -20f;
            whiteB.tint.value = 0f;
            adjust.postExposure.value = 0.19f;
            adjust.contrast.value = 7f;
            bloom.intensity.value = 1.3f;
        }
        else if (currentRound == 2)
        {
            volcanoMap.SetActive(true);
            RenderSettings.skybox = volcanoSkybox;
            ApplyIceMaterial(iceMaterial2);
            iceCollider.material = obsidianPhysics;
            rock.SetIceEffectColor(Color.black);
            rock.fragileIceEnabled = false;
            rock.lavaEnabled = true;

            sun.transform.rotation = Quaternion.Euler(40, 100, 0);
            split.balance.value = -25f;
            whiteB.temperature.value = 86f;
            whiteB.tint.value = 0f;
            bloom.intensity.value = 9f;
            adjust.postExposure.value = -0.5f;
            adjust.contrast.value = 35f;
        }
        else if (currentRound == 3)
        {
            cloudMap.SetActive(true);
            RenderSettings.skybox = cloudSkybox;
            ApplyIceMaterial(iceMaterial3);
            iceCollider.material = cloudPhysics;
            rock.SetIceEffectColor(Color.white);
            rock.fragileIceEnabled = false;
            rock.lavaEnabled = false;

            sun.transform.rotation = Quaternion.Euler(175, 0, 0);
            bloom.intensity.value = 0.5f;
            split.balance.value = 28f;
            whiteB.temperature.value = 16f;
            whiteB.tint.value = 54f;
            adjust.postExposure.value = 0f;
            adjust.contrast.value = 27f;
        }
        DynamicGI.UpdateEnvironment();
    }

    void ApplyIceMaterial(Material mat)
    {
        iceRenderer.material = mat;
        outerSeparatorRenderer.material = mat;
        innerSeparatorRenderer.material = mat;
    }

    IEnumerator InitDelayed()
    {
        yield return null;
        StartTurn();
    }

    void StartTurn()
    {
        // prepare rock for next player turn and reset position
        rock.isRoundEnding = false;
        rock.crossedLine = false;
        UpdateTurnUI();
        UpdateRoundUI();

        Debug.Log("Starting turn for: " + currentTeam);

        rock.SetTeam(currentTeam);

        // assign team color to rocks (Team A: red, Team B: blue)
        if (currentTeam == Team.TeamA)
            rock.SetTeamColor(Color.red);
        else
            rock.SetTeamColor(Color.blue);

        if (rock.rockRenderer != null)
        {
            Material mat = rock.rockRenderer.material;

            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
        }

        rock.transform.position = spawnPoint.position;
        rock.ResetRock();

        // subscribe to event when rock stops moving
        rock.OnRockStopped = HandleRockStopped;
    }

    void HandleRockStopped(Vector3 pos, Quaternion rot, Team team)
    {
        StartCoroutine(RoundEndSequence(pos, rot, team));
    }

    int CalculateScore(Team winningTeam)
    {
        // find closest opponent rock to center for scoring comparison
        float closestOpponentDist = Mathf.Infinity;
        bool opponentHasRock = false;

        foreach (GameObject rockObj in scoringZone.rocksInZone)
        {
            Team team = GetRockTeam(rockObj);

            if (team != winningTeam)
            {
                opponentHasRock = true;

                float dist = Vector3.Distance(
                    rockObj.transform.position,
                    InnerSeparator.position
                );

                if (dist < closestOpponentDist)
                    closestOpponentDist = dist;
            }
        }

        if (scoringZone.rocksInZone == null || scoringZone.rocksInZone.Count == 0)
            return 0;

        // if opponent has no stones in zone, all winning stones score
        if (!opponentHasRock)
        {
            int count = 0;

            // count how many winning stones are closer than opponent’s best stone
            foreach (GameObject rockObj in scoringZone.rocksInZone)
            {
                if (GetRockTeam(rockObj) == winningTeam)
                    count++;
            }

            return count;
        }

        int points = 0;

        foreach (GameObject rockObj in scoringZone.rocksInZone)
        {
            Team team = GetRockTeam(rockObj);

            if (team == winningTeam)
            {
                float dist = Vector3.Distance(
                    rockObj.transform.position,
                    InnerSeparator.position
                );

                if (dist < closestOpponentDist)
                    points++;
            }
        }

        return points;
    }

    void UpdateRoundUI()
    {
        if (roundText != null)
            roundText.text = "Round " + currentRound + "/" + maxRounds;
    }

    void HighlightScoringRocks(Team winningTeam)
    {
        float closestOpponentDist = Mathf.Infinity;
        bool opponentHasRock = false;

        foreach (GameObject rockObj in scoringZone.rocksInZone)
        {
            Team team = GetRockTeam(rockObj);

            if (team != winningTeam)
            {
                opponentHasRock = true;

                float dist = Vector3.Distance(
                    rockObj.transform.position,
                    InnerSeparator.position
                );

                if (dist < closestOpponentDist)
                    closestOpponentDist = dist;
            }
        }

        foreach (GameObject rockObj in scoringZone.rocksInZone)
        {
            Team team = GetRockTeam(rockObj);

            if (team == winningTeam)
            {
                float dist = Vector3.Distance(
                    rockObj.transform.position,
                    InnerSeparator.position
                );

                bool isScoring = !opponentHasRock || dist < closestOpponentDist;

                if (isScoring)
                {
                    Renderer r = rockObj.GetComponentInChildren<Renderer>();
                    if (r != null)
                    {
                        foreach (var mat in r.materials)
                        {
                            mat.EnableKeyword("_EMISSION");
                            mat.SetColor("_EmissionColor", Color.yellow * 2f);
                        }
                    }
                }
            }
        }
    }

    void ClearHighlights()
    {
        foreach (GameObject rockObj in spawnedRocks)
        {
            Renderer r = rockObj.GetComponentInChildren<Renderer>();
            if (r != null)
            {
                foreach (var mat in r.materials)
                {
                    mat.DisableKeyword("_EMISSION");
                }
            }
        }
    }

    IEnumerator RoundEndSequence(Vector3 pos, Quaternion rot, Team team)
    {
        Debug.Log("Round End Start");

        rock.isRoundEnding = true;

        yield return new WaitForSeconds(2f);

        rock.ResetRock();

        GameObject spawned = null;

        if (rock.crossedLine)
        {
            // only valid rocks become part of scoring field
            spawned = Instantiate(rock.SpawnedRockPrefab, pos, rot);
            spawnedRocks.Add(spawned);
            spawned.name = team.ToString();

            Renderer renderer = spawned.GetComponentInChildren<Renderer>();

            if (renderer != null)
            {
                Color color = (team == Team.TeamA) ? Color.red : Color.blue;

                foreach (var mat in renderer.materials)
                {
                    mat.color = color;
                    mat.SetColor("_BaseColor", color);
                }
            }
        }
        else
        {
            Debug.Log("Rock failed line check - discarded");
        }

        if (team == Team.TeamA) teamAStones++;
        else teamBStones++;

        UpdateScoreUI();

        if (teamAStones >= maxStonesPerTeam && teamBStones >= maxStonesPerTeam)
        {
            GameObject closest = GetClosestRockInZone();

            if (closest == null)
            {
                roundEndText.gameObject.SetActive(true);
                roundEndText.text = "Round End\nNo stones in the house - no points scored!";
            }
            else
            {
                Team winner = GetRockTeam(closest);
                int points = CalculateScore(winner);

                if (points == 0)
                {
                    roundEndText.gameObject.SetActive(true);
                    roundEndText.text = "Round End\nNo points scored this round";
                }
                else
                {
                    if (winner == Team.TeamA) teamAScore += points;
                    else teamBScore += points;

                    UpdateScoreUI();
                    ShowRoundResult(winner, points);
                    HighlightScoringRocks(winner);
                }
            }

            rock.mainCamera.enabled = false;
            rock.shotCamera.enabled = true;

            roundEndText.gameObject.SetActive(true);

            yield return new WaitForSeconds(5f);

            roundEndText.gameObject.SetActive(false);

            teamAStones = 0;
            teamBStones = 0;

            foreach (GameObject rck in spawnedRocks)
                Destroy(rck);

            spawnedRocks.Clear();

            ClearHighlights();

            currentRound++;

            if (currentRound > maxRounds)
            {
                EndGame();
                yield break;
            }

            SetRoundEnvironment();
            UpdateRoundUI();

            ShowRoundHazardText();

            SwitchTeam();
            StartTurn();

            yield break;
        }

        SwitchTeam();
        StartTurn();
    }

    void UpdateScoreUI()
    {
        teamAText.text = "" + teamAScore;
        teamBText.text = "" + teamBScore;
    }

    void UpdateTurnUI()
    {
        turnText.text = (currentTeam == Team.TeamA)
            ? "Team A Turn"
            : "Team B Turn";

        Color c = turnText.color;
        turnText.color = new Color(c.r, c.g, c.b, 1f);

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeTurnText());
    }

    void ShowRoundResult(Team winner, int points)
    {
        roundEndText.gameObject.SetActive(true);

        string teamName = (winner == Team.TeamA) ? "Team A" : "Team B";
        roundEndText.text = $"Round End\n{teamName} gets {points} point(s)!";
    }

    void SwitchTeam()
    {
        currentTeam = (currentTeam == Team.TeamA) ? Team.TeamB : Team.TeamA;
        UpdateTurnUI();
    }

    void EndGame()
    {
        Debug.Log("GAME OVER");

        rock.isRoundEnding = true;

        if (endGamePanel != null)
            endGamePanel.SetActive(true);

        string winner;

        if (teamAScore > teamBScore)
            winner = "Team A Wins!";
        else if (teamBScore > teamAScore)
            winner = "Team B Wins!";
        else
            winner = "Tie Game!";

        if (winnerText != null)
            winnerText.text = winner;

        if (finalScoreText != null)
            finalScoreText.text = $"Final Score\nTeam A: {teamAScore}\nTeam B: {teamBScore}";
    }

    public void RestartGame()
    {
        teamAScore = 0;
        teamBScore = 0;

        teamAStones = 0;
        teamBStones = 0;

        currentRound = 1;
        currentTeam = Team.TeamA;

        if (controlPanel != null)
            controlPanel.SetActive(true);

        foreach (GameObject rck in spawnedRocks)
            Destroy(rck);

        spawnedRocks.Clear();

        if (endGamePanel != null)
            endGamePanel.SetActive(false);

        if (hazardText1 != null)
            hazardText1.gameObject.SetActive(false);

        if (hazardText2 != null)
            hazardText2.gameObject.SetActive(false);

        if (hazardText3 != null)
            hazardText3.gameObject.SetActive(false);

        rock.isRoundEnding = false;
        rock.ResetRock();

        UpdateScoreUI();
        UpdateRoundUI();
        UpdateTurnUI();

        SceneManager.LoadScene("MainMenu");
    }

    GameObject GetClosestRock()
    {
        GameObject closest = null;
        float closestDist = Mathf.Infinity;

        foreach (GameObject rockObj in spawnedRocks)
        {
            float dist = Vector3.Distance(rockObj.transform.position, InnerSeparator.position);

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = rockObj;
            }
        }

        return closest;
    }

    GameObject GetClosestRockInZone()
    {
        if (scoringZone.rocksInZone.Count == 0)
            return null;

        GameObject closest = null;
        float closestDist = Mathf.Infinity;

        foreach (GameObject rockObj in scoringZone.rocksInZone)
        {
            float dist = Vector3.Distance(
                rockObj.transform.position,
                InnerSeparator.position
            );

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = rockObj;
            }
        }

        return closest;
    }

    IEnumerator FadeTurnText()
    {
        yield return new WaitForSeconds(1f);

        float duration = 1f;
        float elapsed = 0f;

        Color startColor = turnText.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);

            turnText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        turnText.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
    }

    void ShowRoundHazardText()
    {
        if(hazardFadeRoutine != null)
            StopCoroutine(hazardFadeRoutine);


        if(currentRound == 1 && hazardText1 != null)
        {
            hazardFadeRoutine = StartCoroutine(
                FadeHazardText(hazardText1)
            );
        }

        else if(currentRound == 2 && hazardText2 != null)
        {
            hazardFadeRoutine = StartCoroutine(
                FadeHazardText(hazardText2)
            );
        }

        else if(currentRound == 3 && hazardText3 != null)
        {
            hazardFadeRoutine = StartCoroutine(
                FadeHazardText(hazardText3)
            );
        }
    }

    IEnumerator FadeHazardText(TMP_Text text)
    {
        text.gameObject.SetActive(true);

        Color startColor = text.color;
        startColor.a = 1f;
        text.color = startColor;

        yield return new WaitForSeconds(1f);

        float duration = 1f;
        float elapsed = 0f;

        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float alpha = Mathf.Lerp(1f,0f,elapsed/duration);

            text.color = new Color(
                startColor.r,
                startColor.g,
                startColor.b,
                alpha
            );
            yield return null;
        }
        text.gameObject.SetActive(false);
    }

    public enum Team
    {
        TeamA,
        TeamB
    }

    Team GetRockTeam(GameObject rockObj)
    {
        if (rockObj.name == "TeamA") return Team.TeamA;
        return Team.TeamB;
    }
}