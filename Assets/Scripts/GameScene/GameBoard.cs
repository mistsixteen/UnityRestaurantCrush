﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

enum GameStatus
{
    startingGame,
    Idle,
    FlipMoving,
    Moving,
    MatchCheck,
    FallCheck,
    Falling,
    GameEnded
}

public enum NodeType
{
    None,
    Red,
    Blue,
    Green,
    Yellow
}

public enum MoveType
{
    Up,
    Down,
    Left,
    Right
}

struct NodeContainer
{
    public Node nodeObj;
    public NodeType nodeType;
}

public class GameBoard : MonoBehaviour
{

    //Board Information

    private StageInfo currentStage;

    private Text scoreText, moveText, gameOverText;


    private bool isWaiting = false;

    private List<Node> onMoveList;
    
    NodeContainer[,] NodeBoard;
    bool[,] isMatched;

    GameStatus currentGameState;
    int touchedXpos, touchedYpos;

    public GameBoard()
    {
        
    }

    // Start is called before the first frame update
    void Start()
    {
        currentStage = StageLoadManager.GetInstance().GetStageInfo();
        currentGameState = GameStatus.startingGame;
        onMoveList = new List<Node>();
        scoreText = GameObject.Find("CurrentScore").GetComponent<Text>();
        moveText = GameObject.Find("MoveLeft").GetComponent<Text>();
        gameOverText = GameObject.Find("GameOverUI").GetComponent<Text>();
        gameOverText.gameObject.SetActive(false);
        InitializeBoard();
    }

    void setUI()
    {
        string fmt = "00000000";
        scoreText.text = currentStage.Score.ToString(fmt);
        moveText.text = currentStage.MoveLeft.ToString(fmt);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (isWaiting == true)
            return;
        switch(currentGameState)
        {
            case GameStatus.FlipMoving:
                onMoveList.RemoveAll(item => item.IsIdle() == true);
                if(onMoveList.Count == 0)
                {
                    currentGameState = GameStatus.Idle;
                }
                break;
            case GameStatus.Moving:
                onMoveList.RemoveAll(item => item.IsIdle() == true);
                if (onMoveList.Count == 0)
                {
                    currentGameState = GameStatus.MatchCheck;
                }
                break;
            case GameStatus.MatchCheck:
                if(MakeThreeMatchList() == true)
                {
                    for (int i = 0; i < currentStage.BoardYSize; i++)
                    {
                        for (int j = 0; j < currentStage.BoardXSize; j++)
                        {
                            if(isMatched[i, j] == true)
                            {
                                Node temp = NodeBoard[i, j].nodeObj;
                                temp.SetDisappear();
                                NodeBoard[i, j].nodeObj = null;
                                NodeBoard[i, j].nodeType = NodeType.None;
                                currentStage.Score++;
                            }
                        }
                    }
                    AudioManager.instance.PlayBreakSound();
                    setUI();
                    currentGameState = GameStatus.FallCheck;
                }
                else
                {
                    if (currentStage.MoveLeft <= 0)
                    {
                        for (int i = 0; i < currentStage.BoardYSize; i++)
                        {
                            for (int j = 0; j < currentStage.BoardXSize; j++)
                            {
                                Node temp = NodeBoard[i, j].nodeObj;
                                temp.SetDisappear();
                                NodeBoard[i, j].nodeObj = null;
                                NodeBoard[i, j].nodeType = NodeType.None;
                            }
                        }
                        gameOverText.gameObject.SetActive(true);
                        currentGameState = GameStatus.GameEnded;
                    }
                    else
                        currentGameState = GameStatus.Idle;
                }
                break;
            case GameStatus.FallCheck:
                MakeFallMoveMent();
                if (onMoveList.Count == 0)
                {
                    isWaiting = true;
                    StartCoroutine(WaitBeforenextAction());
                    currentGameState = GameStatus.MatchCheck;
                }
                else
                    currentGameState = GameStatus.Falling;
                break;
            case GameStatus.Falling:
                onMoveList.RemoveAll(item => item.IsIdle() == true);
                if (onMoveList.Count == 0)
                {
                    currentGameState = GameStatus.FallCheck;
                }
                break;
            default:
                break;

        }
    }

    //현재 이동가능한 상황인지 확인
    public bool IsClickAble()
    {
        if(currentGameState == GameStatus.Idle)
            return true;
        return false;
    }

    public void TouchedNode(int xPos, int yPos)
    {
        touchedXpos = xPos;
        touchedYpos = yPos;
    }

    public void ReleasedNode(int xPos, int yPos)
    {
        if (touchedXpos == xPos && touchedYpos == yPos)
        {
            touchedXpos = -1;
            touchedYpos = -1;
        }
    }

    public void MovedNode(int xPos, int yPos, MoveType mType)
    {
        int mXPos = xPos;
        int mYPos = yPos;

        if (touchedXpos == xPos && touchedYpos == yPos)
        {
            switch (mType)
            {
                case MoveType.Up:
                    if (yPos > 0)
                    {
                        mYPos = yPos - 1;
                    }
                    else
                        return;
                    break;
                case MoveType.Down:
                    if (yPos < currentStage.BoardYSize - 1)
                    {
                        mYPos = yPos + 1;
                    }
                    else
                        return;
                    break;
                case MoveType.Left:
                    if (xPos > 0)
                    {
                        mXPos = xPos - 1;
                    }
                    else
                        return;
                    break;
                case MoveType.Right:
                    if (xPos < currentStage.BoardXSize - 1)
                    {
                        mXPos = xPos + 1;
                    }
                    else
                        return;
                    break;
            }

            if (NodeBoard[yPos, xPos].nodeObj == null || NodeBoard[mYPos, mXPos].nodeObj == null
                || NodeBoard[yPos, xPos].nodeObj.CanMove == false || NodeBoard[mYPos, mXPos].nodeObj.CanMove == false)
                return;

            if (MoveThreeMatchCheck(xPos, yPos, mXPos, mYPos)) //3-matched
            {
                Vector3 temp1 = GetNodePosition(mXPos, mYPos);
                Vector3 temp2 = GetNodePosition(xPos, yPos);
                NodeBoard[yPos, xPos].nodeObj.OrderMove(temp1);
                NodeBoard[mYPos, mXPos].nodeObj.OrderMove(temp2);
                onMoveList.Add(NodeBoard[yPos, xPos].nodeObj);
                onMoveList.Add(NodeBoard[mYPos, mXPos].nodeObj);
                NodeContainer temp = NodeBoard[yPos, xPos];
                NodeBoard[yPos, xPos] = NodeBoard[mYPos, mXPos];
                NodeBoard[mYPos, mXPos] = temp;
                NodeBoard[yPos, xPos].nodeObj.SetPosition(xPos, yPos);
                NodeBoard[mYPos, mXPos].nodeObj.SetPosition(mXPos, mYPos);
                currentGameState = GameStatus.Moving;
                currentStage.MoveLeft--;
                setUI();
                //AudioManager.instance.PlayMoveSound();
            }
            else //non -> flip
            {
                Vector3 temp1 = GetNodePosition(mXPos, mYPos);
                Vector3 temp2 = GetNodePosition(xPos, yPos);
                NodeBoard[yPos, xPos].nodeObj.OrderMove(temp1);
                NodeBoard[yPos, xPos].nodeObj.OrderMove(temp2);
                NodeBoard[mYPos, mXPos].nodeObj.OrderMove(temp2);
                NodeBoard[mYPos, mXPos].nodeObj.OrderMove(temp1);
                onMoveList.Add(NodeBoard[yPos, xPos].nodeObj);
                onMoveList.Add(NodeBoard[mYPos, mXPos].nodeObj);
                currentGameState = GameStatus.FlipMoving;

                //Move sound 적당한걸 못 찾음!!
                //AudioManager.instance.PlayMoveSound();
            }

        }
        
    }

    IEnumerator WaitBeforenextAction()
    {
        yield return new WaitForSeconds(0.3f);
        isWaiting = false;
    }

    bool MoveThreeMatchCheck(int xPos1, int yPos1, int xPos2, int yPos2)
    {
        NodeType[,] currentBoard = new NodeType[currentStage.BoardYSize, currentStage.BoardXSize];
        for(int i = 0; i < currentStage.BoardYSize; i++)
        {
            for(int j = 0; j < currentStage.BoardXSize; j++)
            {
                currentBoard[i, j] = NodeBoard[i, j].nodeType;
            }
        }
        NodeType temp = currentBoard[yPos1, xPos1];
        currentBoard[yPos1, xPos1] = currentBoard[yPos2, xPos2];
        currentBoard[yPos2, xPos2] = temp;

        for (int i = 0; i < currentStage.BoardYSize; i++)
        {
            for (int j = 0; j < currentStage.BoardXSize; j++)
            {
                if (i < currentStage.BoardYSize - 2 && 
                    currentBoard[i, j] == currentBoard[i + 1, j] &&
                    currentBoard[i + 1, j] == currentBoard[i + 2, j])
                    return true;
                if (j < currentStage.BoardXSize - 2 &&
                    currentBoard[i, j] == currentBoard[i, j + 1] &&
                    currentBoard[i, j + 1] == currentBoard[i, j + 2])
                    return true;
            }
        }

        return false;
    }
    bool MakeThreeMatchList()
    {
        NodeType[,] currentBoard = new NodeType[currentStage.BoardYSize, currentStage.BoardXSize];

        bool isMatchMade = false;

        for (int i = 0; i < currentStage.BoardYSize; i++)
        {
            for (int j = 0; j < currentStage.BoardXSize; j++)
            {
                currentBoard[i, j] = NodeBoard[i, j].nodeType;
                isMatched[i, j] = false;
            }
        }

        for (int i = 0; i < currentStage.BoardYSize; i++)
        {
            for (int j = 0; j < currentStage.BoardXSize; j++)
            {
                if (currentBoard[i, j] == NodeType.None)
                    continue;

                if (i < currentStage.BoardYSize - 2 &&
                    currentBoard[i, j] == currentBoard[i + 1, j] &&
                    currentBoard[i + 1, j] == currentBoard[i + 2, j])
                {
                    isMatched[i, j] = true;
                    isMatched[i + 1, j] = true;
                    isMatched[i + 2, j] = true;
                    isMatchMade = true;
                }
                if (j < currentStage.BoardXSize - 2 &&
                    currentBoard[i, j] == currentBoard[i, j + 1] &&
                    currentBoard[i, j + 1] == currentBoard[i, j + 2])
                {
                    isMatched[i, j] = true;
                    isMatched[i, j + 1] = true;
                    isMatched[i, j + 2] = true;
                    isMatchMade = true;
                }
                    
            }
        }

        return isMatchMade;
    }
    void MakeFallMoveMent()
    {
        for (int i = currentStage.BoardYSize - 1; i > 0; i--)
        {
            for (int j = 0; j < currentStage.BoardXSize; j++)
            {
                if(NodeBoard[i, j].nodeObj == null && NodeBoard[i-1, j].nodeObj != null && NodeBoard[i-1, j].nodeObj.CanMove == true)
                {
                    NodeBoard[i - 1, j].nodeObj.OrderMove(GetNodePosition(j, i));
                    NodeBoard[i, j] = NodeBoard[i - 1, j];
                    NodeBoard[i - 1, j].nodeObj = null;
                    NodeBoard[i, j].nodeObj.SetPosition(j, i);
                    onMoveList.Add(NodeBoard[i, j].nodeObj);
                }
            }
        }

        for (int j = 0; j < currentStage.BoardXSize; j++)
        {
            if (NodeBoard[0, j].nodeObj == null)
            {
                int random = Random.Range(1, 5);

                switch (random)
                {
                    case 1:
                        NodeBoard[0, j].nodeObj = NodeFactory.GetInstance().CreateNode(NodeList.RedNode);
                        NodeBoard[0, j].nodeType = NodeType.Red;
                        break;
                    case 2:
                        NodeBoard[0, j].nodeObj = NodeFactory.GetInstance().CreateNode(NodeList.BlueNode);
                        NodeBoard[0, j].nodeType = NodeType.Blue;
                        break;
                    case 3:
                        NodeBoard[0, j].nodeObj = NodeFactory.GetInstance().CreateNode(NodeList.GreenNode);
                        NodeBoard[0, j].nodeType = NodeType.Green;
                        break;
                    case 4:
                        NodeBoard[0, j].nodeObj = NodeFactory.GetInstance().CreateNode(NodeList.YellowNode);
                        NodeBoard[0, j].nodeType = NodeType.Yellow;
                        break;
                    default:
                        Debug.LogError("Unidentified Node!!!");
                        NodeBoard[0, j].nodeType = NodeType.None;
                        break;
                }

                NodeBoard[0, j].nodeObj.SetPosition(j, 0);

                Transform rect = NodeBoard[0, j].nodeObj.GetTransform();
                rect.SetParent(this.GetComponent<Transform>());
                rect.position = GetNodePosition(j, -1);

                NodeBoard[0, j].nodeObj.OrderMove(GetNodePosition(j, 0));
                onMoveList.Add(NodeBoard[0, j].nodeObj);

            }
        }
    }

    Vector3 GetNodePosition(int xPos, int yPos)
    {
        return new Vector3(currentStage.BaseXPos + (float)xPos * currentStage.NodeXDistance, currentStage.BaseYPos + (float)yPos * currentStage.NodeYDistance, 0.0f);
    }

    bool IsInstallAble(int xPos, int yPos, NodeType currentNodeType)
    {
        if (currentNodeType == NodeType.None)
            return true;
        if (xPos > 1 && NodeBoard[yPos, xPos - 1].nodeType == currentNodeType && NodeBoard[yPos, xPos - 2].nodeType == currentNodeType)
            return false;
        if (yPos > 1 && NodeBoard[yPos - 1, xPos].nodeType == currentNodeType && NodeBoard[yPos - 1, xPos].nodeType == currentNodeType)
            return false;
        return true;
    }

    public void InitializeBoard()
    {
        int random;

        touchedXpos = -1;
        touchedYpos = -1;

        NodeBoard = new NodeContainer[currentStage.BoardYSize, currentStage.BoardXSize];
        isMatched = new bool[currentStage.BoardYSize, currentStage.BoardXSize];

        for (int i = 0; i < currentStage.BoardYSize; i++)
        {
            for (int j = 0; j < currentStage.BoardXSize; j++)
            {
                if (i == currentStage.BoardYSize - 2)
                {
                    //임시 : xNode 테스트용
                    random = 5;
                }
                else
                { 
                    while (true)
                    {
                        random = Random.Range(1, 5);
                        if (IsInstallAble(j, i, (NodeType)(random)) == true)
                            break;
                    }
                }
                
                switch (random)
                {
                    case 1:
                        NodeBoard[i, j].nodeObj = NodeFactory.GetInstance().CreateNode(NodeList.RedNode);
                        NodeBoard[i, j].nodeType = NodeType.Red;
                        break;
                    case 2:
                        NodeBoard[i, j].nodeObj = NodeFactory.GetInstance().CreateNode(NodeList.BlueNode);
                        NodeBoard[i, j].nodeType = NodeType.Blue;
                        break;
                    case 3:
                        NodeBoard[i, j].nodeObj = NodeFactory.GetInstance().CreateNode(NodeList.GreenNode);
                        NodeBoard[i, j].nodeType = NodeType.Green;
                        break;
                    case 4:
                        NodeBoard[i, j].nodeObj = NodeFactory.GetInstance().CreateNode(NodeList.YellowNode);
                        NodeBoard[i, j].nodeType = NodeType.Yellow;
                        break;
                    case 5:
                        NodeBoard[i, j].nodeObj = NodeFactory.GetInstance().CreateNode(NodeList.XNode);
                        NodeBoard[i, j].nodeType = NodeType.None;
                        break;
                    default:
                        Debug.LogError("Unidentified Node!!!");
                        NodeBoard[i, j].nodeType = NodeType.None;
                        break;
                }

                Transform rect = NodeBoard[i, j].nodeObj.GetTransform();
                NodeBoard[i, j].nodeObj.SetPosition(j, i);

                rect.SetParent(this.GetComponent<Transform>());

                rect.position = GetNodePosition(j, i);
            }
        }
        currentGameState = GameStatus.Idle;
        setUI();
    }
}
