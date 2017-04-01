using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

//test github.

namespace Project0
{
    public partial class MainForm : Form
    {

        public const int ALGORITHM1 = 0;
        public const int ALGORITHM2 = 1;
        

        int[,,] sudokuArray = new int[9, 9, 10];    
                            //9x9 array of 10 slots.  
                            //0th slot: stores the value (1-9) of a solved square. 0 indicates square is as yet unsolved.
                            //so if array[i,j,0] == 0, the square is unsolved.   and we will use slots 1-9 to solve the square.
                            //slots 1-9 initialized to {1,1,1,1,1,1,1,1,1} for unsolved squares - indicating that 1-9 are all valid entries for a particular square.
                            //based on the row, column and nonet information, we will attempt to whittle it down to only 1 "1" with the rest "0"s.
                            //for example: {0,0,0,1,1,0,0,0,0} for slots 1-9 would indicate that the only valid choices for a square are 4 and 5.

        TextBox[,] txtBoxArray = new TextBox[9, 9];

        public MainForm()
        {
            InitializeComponent();
            openFileDialog1.InitialDirectory = Application.StartupPath;           

            //add the 9x9 textboxes to an array, for easier reference later.     
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    txtBoxArray[i, j] = (TextBox)this.Controls.Find("textBox_" + i.ToString() + j.ToString(), true).FirstOrDefault();
                }
            }

            //by default, load first puzzle.
            //String puzzle1 = "D:\\oldfiles\\Desktop\\ksu\\2017 Spring\\ai\\Project0\\Project0\\sudoku219.txt";
            //LoadSudokuFile(puzzle1);

        }

        //precondition:  filename = name of the sudoku file to solve.
        //postcondition: returns true if puzzle loaded into array[,] and onto the screen, into txtBoxArray[,].
        //               returns false if invalid file/puzzle could not be loaded.    
        private bool LoadSudokuFile(String filename)
        {

            System.IO.StreamReader sr = new System.IO.StreamReader(filename);
            //Console.Write("\n" + filename + "\n");
            //MessageBox.Show(sr.ReadToEnd());

            string lineOfText;
            int currVal;

            //load the Sudoku file into the array.
            for (int i = 0; i < 9; i++)
            {
                if ((lineOfText = sr.ReadLine()) != null)
                {
                    string[] values = lineOfText.Split(',');
                    //MessageBox.Show(values.Length.ToString());
                    if (values.Length == 9)
                    {
                        for (int j = 0; j < 9; j++)
                        {
                            currVal = int.Parse(values[j]);
                            sudokuArray[i, j, 0] = currVal;
                            if (currVal == 0)
                            { 
                                //unsolved square - so initialize slots 1-9 to 1.  
                                //(we will use slots 1-9 to solve the square).                                                              
                                for (int k = 1; k < 10; k++)
                                {
                                    sudokuArray[i, j, k] = 1;
                                }
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Invalid sudoku file: need 9 comma separated numbers per line.");
                        return false;
                    }
                }
                else
                {
                    MessageBox.Show("Invalid sudoku file: unexpected end of file.");
                    return false;
                }
            }
            sr.Close();

            drawPuzzleToScreen();

            //if we made it this far, a puzzle loaded successfully.
            //enable the solver buttons.
            solveFor1IterationButton.Enabled = true;
            SolvePuzzleBtn.Enabled = true;
            solveAlg21IterButton.Enabled = true;
            solveAlg2FullButton.Enabled = true;
            return true;
        }


        //postcondition: current state of the puzzle is output to the screen (into the textboxes).
        private void drawPuzzleToScreen()
        {
            int currVal;

            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    currVal = sudokuArray[i, j, 0];
                    if (currVal != 0)
                        txtBoxArray[i, j].Text = currVal.ToString();
                    else
                        txtBoxArray[i, j].Text = "";
                }
            }
        }

        //postcondition: returns FALSE if no changes made to sudoku array (we can stop running the puzzle solver).
        //               returns TRUE if sudoku array was updated (we should continue calling this method till we reach a solution).
        private bool SolvePuzzleSingleIteration(int algorithm)
        {
            int currVal, numChoices, solvedVal;
            bool updated = false;
            for (int i = 0; i < 9; i++) //for each row
            {
                for (int j = 0; j < 9; j++) //for each column
                {
                    currVal = sudokuArray[i, j, 0];
                    if (currVal != 0)
                    {
                        updateSquaresInThisRow(currVal, i);
                        updateSquaresInThisColumn(currVal, j);
                        updateSquaresInThisNonet(currVal, i, j);
                    }
                }
            }
         
            //now, search the array for the unsolved squares that have only 1 possible choice remaining.
            //if we find them, update the sudokuArray[i,j,0] to indicate that these squares have been solved.         
            for (int i = 0; i < 9; i++) //for each row
            {
                for (int j = 0; j < 9; j++) //for each column
                {
                    currVal = sudokuArray[i, j, 0];
                    if (currVal == 0)
                    {
                        numChoices = 0;
                        solvedVal = 0;
                        for (int k = 1; k < 10; k++)
                        {
                            if (sudokuArray[i, j, k] == 1)
                            {
                                numChoices++;
                                solvedVal = k;
                            }

                        }
                        if (numChoices == 0)
                            MessageBox.Show("Logic error in array position[" + i.ToString() + "," + j.ToString() + "! Unsolved square should never have 0 remaining choices.");
                        else if (numChoices==1)
                        {
                            sudokuArray[i, j, 0] = solvedVal;
                            updated = true;
                        }
                    }
                }
            }
        
            if (algorithm == ALGORITHM2)      
            {
                //additional code below allows us to solve some medium-level sudoku puzzles.
                //(this is Step 3 in the algorithm discussed in the report.)
                //for each nonet, 
                //      for each unplaced integer i(1-9) in the nonet,
                //          check if there is only one valid location for that integer i to be placed in the nonet.
                for (int m = 0; m < 3; m++)
                {
                    for (int n = 0; n < 3; n++)
                    {
                        if (checkAndUpdateNonette(m, n))
                            updated = true;
                    }
                }
            }
            
            return updated;
        }

        //postcondition: all squares in this row have 'value' removed from their list of possible entries.
        private void updateSquaresInThisRow(int value, int row)
        {
            for (int k = 0; k < 9; k++)
            {
                sudokuArray[row, k, value] = 0;
            }
        }

        //postcondition: all squares in this column have 'value' removed from their list of possible entries.
        private void updateSquaresInThisColumn(int value, int column)
        {
            for (int l = 0; l < 9; l++)
            {
                sudokuArray[l, column, value] = 0;
            }
        }

        //postcondition: all squares in this nonet have 'value' removed from their list of possible entries.
        private void updateSquaresInThisNonet(int value, int row, int column)
        {
            int xStart = row - row%3;
            int yStart = column - column%3;
            for (int m = 0; m < 3; m++)
            {
                for (int n = 0; n < 3; n++)
                {
                    sudokuArray[m + xStart, n + yStart, value] = 0;
                }
            }
        }

        //for each unplaced integer i(1-9) in the nonet,
        //check if there is only one valid location for that integer i to be placed in the nonet.
        //(this is Step 3 in the algorithm discussed in the report.)
        //postcondition: returns FALSE if no changes made to nonette.
        //               returns TRUE if nonette was updated.  
        private bool checkAndUpdateNonette(int r, int c)
        {
            bool updated = false;

            int spotCounter;
            int spotX, spotY;

            SortedSet<int> setOfIntsInNonette = new SortedSet<int>();
            //first, identify which integers have already been placed in the nonette.
            for (int i = r * 3; i < r * 3 + 3; i++)
            {
                for (int j = c * 3; j < c * 3 + 3; j++)
                {
                    if (sudokuArray[i, j, 0] != 0)
                    {
                        setOfIntsInNonette.Add(sudokuArray[i, j, 0]);
                    }
                }
            }
            
            for (int n = 1; n < 10; n++)
            {  //for each integer (1-9)

                if (!setOfIntsInNonette.Contains(n))  //only search for a spot if the integer has not been placed on the board already.
                {  
                    spotCounter = 0;
                    spotX = -1;
                    spotY = -1;

                    //search the nonette for valid spot.  if we find a valid spot, increment the spotCounter.
                    for (int i = r * 3; i < r * 3 + 3; i++)
                    {
                        for (int j = c * 3; j < c * 3 + 3; j++)
                        {
                            if (sudokuArray[i, j, 0] == 0)    //if != 0, spot is solved/taken.
                            {
                                if (sudokuArray[i, j, n] == 1)
                                {
                                    spotCounter++;
                                    spotX = i;
                                    spotY = j;
                                }

                            }
                        }
                    }

                    //if we have only one possible spot, let's fill it in! another location solved.
                    if (spotCounter == 1)
                    {
                        sudokuArray[spotX, spotY, 0] = n;
                        updated = true;
                    }

                }
            }
            return updated;
        }


        private void OpenFileButton_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LoadSudokuFile(openFileDialog1.FileName);

            }
  

        }

        private void solveFor1IterationBtn_Click(object sender, EventArgs e)
        {
            if (!SolvePuzzleSingleIteration(ALGORITHM1))
            {
                MessageBox.Show("Solver is finished!");
            }
            drawPuzzleToScreen();
        }

        private void solveAlg21IterButton_Click(object sender, EventArgs e)
        {
            if (!SolvePuzzleSingleIteration(ALGORITHM2))
            {
                MessageBox.Show("Solver is finished!");
            }
            drawPuzzleToScreen();
        }

        private void SolvePuzzleBtn_Click(object sender, EventArgs e)
        {
            int before = countSolvedSquares();
            solvePuzzleFull(ALGORITHM1);
            int after = countSolvedSquares();
            Console.WriteLine((after - before).ToString() + " squares solved.");
        }


        private void solveAlg2FullButton_Click(object sender, EventArgs e)
        {
            int before = countSolvedSquares();
            solvePuzzleFull(ALGORITHM2);
            int after = countSolvedSquares();
            Console.WriteLine((after - before).ToString() + " squares solved.");
        }

        private void solvePuzzleFull(int algorithm)
        {
            Stopwatch watch = Stopwatch.StartNew();

            bool stillUpdating = true;
            while (stillUpdating)
            {
                stillUpdating = SolvePuzzleSingleIteration(algorithm);
            }
            drawPuzzleToScreen();

            watch.Stop();
            Console.WriteLine("Puzzle solved in: " + watch.Elapsed.ToString() + " seconds");
        }

        //postcondition: returns the number of solved squares in the sudokuArray.
        private int countSolvedSquares ()
        {
            int counter = 0;
            for (int i =0; i < 9; i++)
            {
                for (int j=0; j < 9; j++)
                {
                    if (sudokuArray[i, j, 0] != 0)
                        counter++;
                }
            }
            return counter;
        }
    }

    

}
