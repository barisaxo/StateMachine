using System;
using UnityEngine;
using Bard2D.Boards;
using Bard2D.HUDs;
using Bard2D.AudioSystems;
using System.Threading.Tasks;

public abstract class State
{
    #region REFERENCES
    protected static ButtonHUD BHUD => ButtonHUD.Io;
    protected static Board Board => Board.Io;
    protected static AudioManager Audio => AudioManager.Io;
    protected static DataManager Data => DataManager.Io;
    #endregion REFERENCES


    #region STATE SYSTEMS
    /// <summary>
    /// Called by SetStateDirectly() and InitiateFade().
    /// </summary>
    protected void DisableInput()
    {
        InputKey.ButtonEvent -= GPInput;
        InputKey.StickEvent -= GPInput;
        InputKey.RStickAltEvent -= GPInput;
        InputKey.MouseClickEvent -= Clicked;
        InputKey.SpaceBarEvent -= SpaceBar;
    }

    /// <summary>
    /// Called by SetStateDirectly() and FadeOutToBlack().
    /// </summary>
    protected virtual void DisengageState() { }

    /// <summary>
    /// Called by SetStateDirectly() and FadeOutToBlack(). Don't set new states here.
    /// </summary>
    protected virtual void PrepareState(Action callback) { callback(); }

    /// <summary>
    /// Called by SetSceneDirectly() and FadeInToScene().
    /// </summary>
    protected void EnableInput()
    {
        InputKey.ButtonEvent += GPInput;
        InputKey.StickEvent += GPInput;
        InputKey.RStickAltEvent += GPInput;
        InputKey.MouseClickEvent += Clicked;
        InputKey.SpaceBarEvent += SpaceBar;
    }

    /// <summary>
    /// Called by SetStateDirectly() and FadeInToScene(). OK to set new states here.
    /// </summary>
    protected virtual void EngageState() { }

    protected void SetStateDirectly(State newState)
    {
        DisableInput();
        DisengageState();

        newState.PrepareState(Initiate);

        async void Initiate()
        {
            await Task.Yield();

            newState.EnableInput();
            newState.EngageState();
        }
    }

    protected void FadeToState(State newState)
    {
        ScreenFader fader = new ScreenFader();
        InitiateFade(newState);
        return;

        async void InitiateFade(State newState)
        {
            DisableInput();
            await Task.Yield();
            FadeOutToBlack(newState);
        }

        async void FadeOutToBlack(State newState)
        {
            while (fader.Screen.color.a < .99f)
            {
                await Task.Yield();
                if (!Application.isPlaying) return;
                fader.Screen.color += new Color(0, 0, 0, Time.deltaTime * 1.25f);
            }

            fader.Screen.color = Color.black;
            await Task.Yield();
            newState.PrepareState(FadeInToScene);
        }

        async void FadeInToScene()
        {
            DisengageState();

            while (fader.Screen.color.a > .01f)
            {
                await Task.Yield();
                if (!Application.isPlaying) return;
                fader.Screen.color -= new Color(0, 0, 0, Time.deltaTime * 2.0f);
            }

            fader?.SelfDestruct();
            newState.EnableInput();
            newState.EngageState();
        }
    }
    #endregion STATE SYSTEMS


    #region INPUT HANDLING
    protected virtual void SpaceBar(SpaceBarAction action) { }

    protected virtual void Clicked(MouseAction action, Vector2 position)
    {
        if (action == MouseAction.LUp)
        {
            RaycastHit2D hit = Physics2D.Raycast(Cam.Io.Camera.ScreenToWorldPoint(position), Vector2.zero);

            if (hit.collider == null)
            {
                return;
            }

            if (hit.collider.gameObject == BHUD.MuteButton.Parent)
            {
                BHUD.MuteButton.SetTextString(BHUD.MuteButton.TMP.text == "›" ? "≠" : "›");
                AudioListener.volume = BHUD.MuteButton.TMP.text == "›" ? 0 : 1;

                return;
            }

            if (hit.collider.gameObject == BHUD.QuitButton.Parent)
            {
                SetStateDirectly(new DialogStart_State(new Quit_Dialogue(this)));
                return;
            }

            ClickedOn(hit.collider.gameObject);
        }
    }

    protected virtual void ClickedOn(GameObject go) { }

    protected virtual void Holding(GameObject go) { }

    protected virtual void UnClicked() { }

    protected virtual void GPInput(GP_Button gpb)
    {
        Debug.Log(gpb);

        switch (gpb)
        {
            case GP_Button.Up_Press: DirectionPressed(Dir.Up); break;
            case GP_Button.Down_Press: DirectionPressed(Dir.Down); break;
            case GP_Button.Left_Press: DirectionPressed(Dir.Left); break;
            case GP_Button.Right_Press: DirectionPressed(Dir.Right); break;
            case GP_Button.North_Press: InteractPressed(); break;
            case GP_Button.East_Press: ConfirmPressed(); break;
            case GP_Button.South_Press: CancelPressed(); break;
            case GP_Button.Start_Press: StartPressed(); break;
            case GP_Button.Select_Press: SelectPressed(); break;

            case GP_Button.Up_Release: break;
            case GP_Button.Down_Release: break;
            case GP_Button.Left_Release: break;
            case GP_Button.Right_Release: break;
            case GP_Button.North_Release: break;
            case GP_Button.East_Release: break;
            case GP_Button.South_Release: break;
            case GP_Button.Start_Release: break;
            case GP_Button.Select_Release: break;
        };
    }

    protected virtual void GPInput(GP_Button gpb, Vector2 v2)
    {
        Debug.Log(gpb + " " + v2);
    }

    protected virtual void GPInput(GP_Button gpb, float f)
    {
        Debug.Log(gpb + " " + f);
    }

    protected virtual void DirectionPressed(Dir dir) { }

    protected virtual void ConfirmPressed() { }

    protected virtual void InteractPressed() { }

    protected virtual void CancelPressed() { }

    protected virtual void StartPressed() { }

    protected virtual void SelectPressed() { }
    #endregion INPUT HANDLING
}