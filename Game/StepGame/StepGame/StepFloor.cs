﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using NovaDll;
using System.IO;

namespace Step_Game
{
    public class StepFloor
    {
        private Song music;
        public struct Step
        {
            public Double time;
            public int note;
        }
        Stack<Step> steps = new Stack<Step>();

        Texture2D background, scoreBackground;

        private int stepSpeed = 15;
        private int stepSpace = 140;
        private int numberOfSteps = 5;
        private Double timeBeforeCall = 0f;

        private static String MUSIC_NAME = "music.mp3";
        private static String SCRIPT_NAME = "steps.script";
        private static String MUSIC_RESOURCE_NAME = "CURRENT_MUSIC";

        public int notesOk=0;
        private int notesMiss=0;
        private float hitRatio =0f;
        public float totalNotes = 0f;

        SpriteFont scoreFont;
        
        public struct OffNote
        {
            public Texture2D texture;
            public Vector2 pos;
            public Rectangle colision;
        }

        OffNote[] offNotes = new OffNote[5];

        Keys[] controles = new Keys[5]
        {
            Keys.A,
            Keys.S,
            Keys.J,
            Keys.K,
            Keys.L
        };

        KeyboardState keyboard, oldKeyboard;

        private List<Nova_Particle> notes = new List<Nova_Particle>();

        private Matrix matrix = Matrix.CreateTranslation(0, 0, 0);

        public StepFloor(String musicPath, Texture2D background, Texture2D scoreBackground)
        {
           
            Loader.LoadNotes();
            this.scoreBackground = scoreBackground;
            this.background = background;
            scoreFont = (SpriteFont)Nova_DataBase.GetResource("FONT_SCORE");
            LoadMusic(musicPath);
            LoadScript(musicPath);
            LoadSteps();
            LoadOffNotes();
            totalNotes = steps.Count;
            MediaPlayer.Play(music);
        }

        private void LoadOffNotes()
        {
            for (int i = 0; i < 5; i++)
            {
                Texture2D texture = (Texture2D)Nova_DataBase.GetResource("NOTE_OFF_" + (i + 1).ToString());
                Vector2 pos = new Vector2(i * stepSpace + Nova_Functions.View.Width / 2 - ((numberOfSteps * stepSpace) / 2), Nova_Functions.View.Height - texture.Height);
                OffNote on = new OffNote();
                on.pos = pos;
                on.texture = texture;
                on.colision = Nova_Functions.CreateRectangle(pos, texture);
                offNotes[i] = on;
            }
        }

        private void SetTimeBeforeCall(GameTime gameTime)
        {
            if (gameTime.ElapsedGameTime.Milliseconds != 0)
            { 
                Double noteHeightSize = ((Texture2D)Nova_DataBase.GetResource("NOTE_OFF_1")).Height;
                Double distance = Nova_Functions.View.Height + noteHeightSize / 2;
                Double ticksPerSecond = 1000 / gameTime.ElapsedGameTime.Milliseconds;
                Double distancePerSecond = ticksPerSecond * stepSpeed;
                Double timeToGoal = distance / distancePerSecond;
                timeBeforeCall = timeToGoal * 1000;
            }
        }

        private void LoadMusic(String musicPath)
        {
            String d = Nova_Importer.Content.RootDirectory + "\\" + musicPath.Split('\\')[musicPath.Split('\\').Length - 1] + "\\MUSIC";
            Nova_Importer.LoadResource("MUSIC",  musicPath.Split('\\')[musicPath.Split('\\').Length -1] + "\\MUSIC");
            music = (Song)Nova_DataBase.GetResource("MUSIC");
        }

        private void LoadScript(String musicPath)
        {
            String fileLocation = musicPath + "\\" + SCRIPT_NAME;
            StreamReader sr = new StreamReader(fileLocation);
            while (!sr.EndOfStream)
            {
                String line = sr.ReadLine();
                String paramTime = line.Split('|')[0];
                String stepNumberParam = line.Split('|')[1];

                Double time = Double.Parse(paramTime, System.Globalization.NumberStyles.AllowDecimalPoint);
                int step = int.Parse(stepNumberParam);

                Step s = new Step();
                s.time = time;
                s.note = step;

                steps.Push(s);
            }
            sr.Close();
        }

        private void LoadSteps()
        {
            Nova_Importer.LoadResource("particle", "particle");
        }

        private void DrawOffSteps(SpriteBatch render)
        {
            for (int i = 0; i < 5; i++)
            {
                render.Draw(offNotes[i].texture, offNotes[i].pos, Color.White);
            }
        }

        private void CreateStep(int posNumber)
        {
            Nova_Particle p = new Nova_Particle();
            p.SetTexture((List<Texture2D>)Nova_DataBase.GetResource("NOTE_" + (posNumber+1).ToString()), SpriteEffects.None, Color.White);
            p.SetPosition(new Vector2(posNumber * stepSpace + Nova_Functions.View.Width / 2 - ((numberOfSteps * stepSpace) / 2), -p.texture[0].Height));
            p.SetAnimation(4, 1, 120, true);
            p.SetID(posNumber.ToString());
            p.SetDirectionSpeed(new Vector2(0, stepSpeed));
            notes.Add(p);
        }

        private void StepsSender()
        {
            if (steps.Count > 0)
            {
                while (true)
                {
                    if (steps.Count > 0)
                    {
                        Double stepTime = steps.Peek().time * 1000;
                        if (stepTime - timeBeforeCall <= MediaPlayer.PlayPosition.TotalMilliseconds)
                        {
                            Step s = steps.Pop();
                            CreateStep(s.note);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private void DrawNoteHit(int pos)
        {
            Nova_Particle p = new Nova_Particle();
            p.SetTexture((Texture2D)Nova_DataBase.GetResource("NOTE_HIT_" + (pos + 1).ToString()), SpriteEffects.None, Color.White);
            p.SetPosition(offNotes[pos].pos);
            p.SetInflateSpeed(5, 20, 5, 20, Nova_Particle.GrowingType.asc);
            p.SetLifeTime(600);
            p.SetFadeOut(600);
            notes.Add(p);
        }

        private void DrawNoteMiss(int pos)
        {
            Nova_Particle p = new Nova_Particle();
            p.SetTexture((Texture2D)Nova_DataBase.GetResource("NOTE_HIT_" + (pos + 1).ToString()), SpriteEffects.None, Color.Red);
            p.SetPosition(offNotes[pos].pos);
            p.SetInflateSpeed(6, 20, 6, 20, Nova_Particle.GrowingType.asc);
            p.SetLifeTime(300);
            p.SetFadeOut(300);
            notes.Add(p);
        }

        private void UpdateSteps(GameTime gameTime)
        {
            for (int i = 0; i < notes.Count; i++)
            {
                notes[i].Update(gameTime, matrix);
                if (notes[i].GetColisionRectangle().Top > Nova_Functions.View.Height + 50)
                {
                    notes[i].isAlive = false;
                    notesMiss++;
                }
                if (!notes[i].isAlive)
                {
                    notes.Remove(notes[i]);
                    i--;
                }
            }
        }

        private void Controls()
        {
            keyboard = Keyboard.GetState();
            Boolean[] hit = new Boolean[5];
            for (int n = 0; n < notes.Count; n++)
            {
                
                Boolean isNote = false;
                for (int i = 0; i < 5; i++)
                {
                    if (notes[n].ID.Equals(i.ToString()))
                    {
                        isNote = true;
                    }
                }
                if (isNote)
                {
                    int index = Convert.ToInt16(notes[n].ID);
                    if (keyboard.IsKeyDown(controles[index]) && oldKeyboard.IsKeyUp(controles[index]))
                    {
                        if (Nova_Functions.isRectangleIntersection(offNotes[index].colision, notes[n].GetColisionRectangle()))
                        {
                            if (!hit[index])
                            {
                                hit[index] = true;
                                DrawNoteHit(index);
                                notes[n].isAlive = false;
                                notesOk++;
                                if (Menu.serverRunning)
                                {
                                    Menu.client.SendMessage(music.Name + "|" + notesOk.ToString() + "|" + Menu.playerName);
                                }
                            }
                        }
                    }
                }
            }
            oldKeyboard = keyboard;
            
        }

        private void UpdateRatio()
        {
            hitRatio = notesOk / totalNotes * 100;
        }

        private Vector2 margin = new Vector2(26, 30);

        private void DrawHud(SpriteBatch render)
        {
            render.Draw(scoreBackground, new Rectangle(0, 0, 300, 330), Color.White);
            render.DrawString(scoreFont, "Acerto: " + notesOk.ToString() + "/" + totalNotes.ToString(), new Vector2(0, 0) + margin, Color.White);
            render.DrawString(scoreFont, "Erro: " + notesMiss.ToString(), new Vector2(0, 80) + margin, Color.White);
            render.DrawString(scoreFont, "Porcentagem: " + Nova_Functions.FormatNumber(hitRatio, 2) + "%", new Vector2(0, 160) + margin, Color.White);
            if (Menu.serverRunning)
            {
                render.DrawString(scoreFont, otherPlayerName + " :" + otherScore, new Vector2(0, 240) + margin, Color.White);
            }
        }

        public void Update(GameTime gameTime)
        {
            UpdateSteps(gameTime);
            Controls();
            UpdateRatio();
            SetTimeBeforeCall(gameTime);
            StepsSender();
            SendScore();
            GetScore();
        }


        
        String otherScore;

        private void SendScore()
        {
            
        }

        private void GetScore()
        {
            if (Menu.serverRunning)
            {
                String message = Menu.client.GetLastMessage();
                if (message.Contains(music.Name) && message.Split('|')[2] != Menu.playerName)
                {
                    otherScore = message.Split('|')[1];
                    otherPlayerName = message.Split('|')[2];
                }
            }
        }

        public String otherPlayerName = "";

        public void Draw(SpriteBatch render)
        {
            render.Draw(background, Nova_Functions.ReturnScreenRectangle(), Color.White);
            DrawOffSteps(render);
            DrawHud(render);
            Nova_Particle.DoDrawParticles(notes, render);
            
        }
    }
}
