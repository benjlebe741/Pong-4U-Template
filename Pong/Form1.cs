/*
 * Description:     A basic PONG simulator
 * Author:           
 * Date:            
 */

#region libraries

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Media;

#endregion

namespace Pong
{
    public partial class Form1 : Form
    {
        #region global values

        //graphics objects for drawing
        Pen lazerPen = new Pen(Color.Salmon, 2);
        Pen lazerPenUnderlay = new Pen(Color.Red, 3);
        SolidBrush whiteBrush = new SolidBrush(Color.White);
        Font drawFont = new Font("Courier New", 10);

        // Sounds for game
        SoundPlayer scoreSound = new SoundPlayer(Properties.Resources.score);
        SoundPlayer collisionSound = new SoundPlayer(Properties.Resources.collision);

        // check to see if a new game can be started
        Boolean newGameOk = true;

        //Player values *Individual*
        int ballTouching;
        Rectangle[] rect12 = new Rectangle[2];
        Rectangle[] check12 = new Rectangle[2];
        Rectangle[] laserEnd12 = new Rectangle[2];
        int[] score12 = new int[2] { 0, 0 };
        int[] ballChange12 = new int[2] { 1, -1 };
        Label[] label12 = new Label[2];
        int[] up12 = new int[2] { 0, 0 };
        int[] down12 = new int[2] { 0, 0 };
        Keys[] upKey12 = new Keys[2] { Keys.W, Keys.Up };
        Keys[] downKey12 = new Keys[2] { Keys.S, Keys.Down };

        //ball values
        int ballMode = 0;
        Color[] ballModeColor = new Color[2] { Color.White, Color.Red };
        int ballMoveRight = 1;
        int ballMoveDown = 1;
        float ballSpeedHorizontal;
        float ballSpeedVertical;
        const int ballSnipeSpeed = 10;
        const int BALL_WIDTH = 20;
        const int BALL_HEIGHT = 20;
        const int BALL_OFFSET = 20;
        Rectangle ball;

        Point ballMiddle;
        Point[] playerMiddle12 = new Point[2];

        //player values
        const int PADDLE_SPEED = 4;
        const int PADDLE_EDGE = 20;  // buffer distance between screen edge and paddle            
        const int PADDLE_WIDTH = 10;
        const int PADDLE_HEIGHT = 40;

        //laser values
        const int LASER_SPEED = 18;
        const int LASER_OFFSET = 200;
        int laserMoveDown = 1;
        const int LINE_DEVISION = 35;

        //game score
        int gameWinScore = 3;  // number of points needed to win game

        #endregion

        public Form1()
        {
            InitializeComponent();
            label12 = new Label[2] { player1ScoreLabel, player2ScoreLabel };
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            //check to see if a key is pressed and set is KeyDown value to true if it has

            for (int i = 0; i <= 1; i++)
            {
                if (e.KeyCode == upKey12[i]) { up12[i] = -1; }
                if (e.KeyCode == downKey12[i]) { down12[i] = 1; }
            }

            switch (e.KeyCode)
            {
                case Keys.Space:
                    if (newGameOk)
                    {
                        SetParameters();
                    }
                    else if (ballMode != 0)
                    {
                        ballMode = 0;
                        shootBall(ballTouching - 1);
                    }
                    break;
                case Keys.Escape:
                    if (newGameOk)
                    {
                        Close();
                    }
                    break;
            }
        }

        private void shootBall(int shooter) 
        {
            //No Longer Touching
            ballTouching = 0;
            
            //Remove Regular Pong Ball Speeds
            ballSpeedHorizontal = 0;
            ballSpeedVertical = 0;
            ballMoveDown = -1;
            ballMoveRight = -1;

            //Find the slope of the line
            Point lineStart = ballMiddle;
            Point lineEnd = laserEnd12[shooter].Location;

            //Get the Pong Ball to follow the line
            ballSpeedHorizontal = (lineStart.X - lineEnd.X) / LINE_DEVISION;
            ballSpeedVertical = (lineStart.Y - lineEnd.Y) / LINE_DEVISION;
        }
        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            //check to see if a key has been released and set its KeyDown value to false if it has
            for (int i = 0; i <= 1; i++)
            {
                if (e.KeyCode == upKey12[i]) { up12[i] = 0; }
                if (e.KeyCode == downKey12[i]) { down12[i] = 0; }
            }
        }
        private void SetParameters()
        {
            if (newGameOk)
            {
                score12[0] = score12[1] = 0;
                label12[0].Text = label12[1].Text = "";
                newGameOk = false;
                startLabel.Visible = false;
                gameUpdateLoop.Start();
            }
            SetStartingLocations();
        }
        private void SetStartingLocations()
        {
            //player start positions
            rect12[0] = new Rectangle(PADDLE_EDGE, this.Height / 2 - PADDLE_HEIGHT / 2, PADDLE_WIDTH, PADDLE_HEIGHT);
            check12[0] = rect12[0];
            rect12[1] = new Rectangle(this.Width - PADDLE_EDGE - PADDLE_WIDTH, this.Height / 2 - PADDLE_HEIGHT / 2, PADDLE_WIDTH, PADDLE_HEIGHT);
            check12[1] = rect12[1];
            ball = new Rectangle((this.Width / 2) - (BALL_WIDTH / 2), (this.Height / 2) - (BALL_HEIGHT / 2), BALL_WIDTH, BALL_HEIGHT);

            laserEnd12[1] = new Rectangle(0, 0, 0, 0);
            laserEnd12[0] = new Rectangle(this.Width, 0, 0, 0);

            ballSpeedHorizontal = 4;
            ballSpeedVertical = 4;
        }
        private void gameUpdateLoop_Tick(object sender, EventArgs e)
        {
            #region Updates related to Players
            for (int i = 0; i <= 1; i++)
            {
                //Update Middle of Players
                playerMiddle12[i] = new Point(rect12[i].X + (PADDLE_WIDTH / 2), rect12[i].Y + (PADDLE_HEIGHT / 2));
                
                //Update Laser Position
                laserEnd12[i].Y += (laserMoveDown * LASER_SPEED);
            
                //Update Paddle Positions
                check12[i].Location = rect12[i].Location;

                check12[i].Y += (PADDLE_SPEED * (up12[i] + down12[i]));
                if (check12[i].Y > 0 && check12[i].Y < (this.Height - PADDLE_HEIGHT))
                {
                    rect12[i].Y += (PADDLE_SPEED * (up12[i] + down12[i]));
                }

                //Ball Collision with Paddles
                if (ball.IntersectsWith(rect12[i]))
                {
                    collisionSound.Play();
                    ballMoveRight = ballChange12[i];
                    ballTouching = i + 1;
                    ballMode = 1;
                }
            }
            #endregion

            #region update ball position

            if (ballTouching == 0)
            {
                ball.X += Convert.ToInt32(ballMoveRight * ballSpeedHorizontal);
                ball.Y += Convert.ToInt32(ballMoveDown * ballSpeedVertical);
            }
            else
            {
                ballSpeedHorizontal = 4;
                ball.X = (playerMiddle12[ballTouching - 1].X) + ball.X - ballMiddle.X + (BALL_OFFSET * ballChange12[ballTouching - 1]);
                ball.Y = (playerMiddle12[ballTouching - 1].Y) + ball.Y - ballMiddle.Y;
            }

            ballMiddle = new Point(ball.X + (BALL_WIDTH / 2), ball.Y + (BALL_HEIGHT / 2));

            #endregion

            #region collision with top and bottom lines

            if ((ball.Y <= 0) || (ball.Y >= this.Height - BALL_HEIGHT)) // if ball hits top or bottom line
            {
                ballMoveDown *= -1;
                collisionSound.Play();
            }
            if (laserEnd12[0].Y <= 0 - LASER_OFFSET)
            {
                laserMoveDown = 1;
            }
            else if (laserEnd12[0].Y >= this.Height + LASER_OFFSET)
            {
                laserMoveDown = -1;
            }
            #endregion

            #region ball collision with side walls (point scored)
            if (ball.X < 0)  // ball hits left wall logic
            {
                scoreSound.Play();
                score12[1] += 1;
                label12[1].Text = score12[1].ToString();

                if (score12[1] >= gameWinScore)
                {
                    GameOver("Player 2");
                }
                else
                {
                    SetParameters();
                    ballMoveRight *= -1;
                    ballMoveDown *= -1;
                }

            }

            if (ball.X > this.Width - BALL_WIDTH)  // ball hits right wall logic
            {
                scoreSound.Play();
                score12[0] += 1;
                label12[0].Text = score12[0].ToString();

                if (score12[0] >= gameWinScore)
                {
                    GameOver("Player 1");
                }
                else
                {
                    SetParameters();
                    ballMoveRight *= -1;
                    ballMoveDown *= -1;
                }

            }

            #endregion

            //refresh the screen, which causes the Form1_Paint method to run
            this.Refresh();
        }

        /// <summary>
        /// Displays a message for the winner when the game is over and allows the user to either select
        /// to play again or end the program
        /// </summary>
        /// <param name="winner">The player name to be shown as the winner</param>
        private void GameOver(string winner)
        {
            newGameOk = true;
            SetStartingLocations();
            startLabel.Text = "The Winner Is " + (winner) + "\nPlay Again? (space)";
            startLabel.Visible = true;
            Refresh();
            gameUpdateLoop.Enabled = false;
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            for (int i = 0; i <= 1; i++)
            {
                e.Graphics.FillRectangle(whiteBrush, rect12[i]);

                if (ballTouching - 1 == i)
                {
                    e.Graphics.DrawLine(lazerPenUnderlay, ballMiddle, laserEnd12[i].Location);
                    e.Graphics.DrawLine(lazerPen, ballMiddle, laserEnd12[i].Location);
                }
            }
            e.Graphics.FillRectangle(new SolidBrush(ballModeColor[ballMode]), ball);
        }

    }
}
