﻿/*****************************************************************************
 * 
 * ReoGrid - .NET Spreadsheet Control
 * 
 * http://reogrid.net/
 *
 * THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
 * KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
 * PURPOSE.
 *
 * Source code in test-case project released under BSD license.
 * Copyright (c) 2012-2016 unvell.com, all rights reserved.
 * 
 ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using unvell.ReoGrid.Formula;

namespace unvell.ReoGrid.Tests
{
	[TestSet]
	class FormulaTest : ReoGridTestSet
	{
		//
		// NOTE: Do not move these test-cases in this class
		//       Test-cases performed have the order
		//

		[TestCase]
		void NormalFormula()
		{
			SetUp();

			worksheet["A1"] = 10;
			worksheet["B1"] = "=A1";
			AssertSame(worksheet["B1"], 10);
			AssertTrue(worksheet.Cells[0, 1].HasFormula);
		}

		[TestCase]
		void ReferenceUpdate()
		{
			// formula after value
			worksheet["C1"] = 10;
			worksheet["D1"] = "=C1";
			worksheet["C1"] = 20;
			AssertSame(worksheet["D1"], 20);

			// formula before value
			worksheet["D1"] = "=E1";
			worksheet["E1"] = 30;
			AssertSame(worksheet["D1"], 30);
		}

		[TestCase]
		void BasicAdd()
		{
			worksheet["F1"] = 10;
			worksheet["G1"] = 20;
			worksheet["H1"] = "=F1+G1";

			AssertSame(worksheet["H1"], 30);
		}

		[TestCase]
		void FormulaResultReference()
		{
			worksheet["I1"] = 40;
			worksheet["J1"] = "=H1+I1";

			AssertSame(worksheet["J1"], 70);
		}

		[TestCase]
		void RefereceInFunction()
		{

#if NO_LONGER_SUPPORTED_V088
			// ReoScript built-in objects and functions

			worksheet[0, 0] = "=Math.floor(Math.sin(0.625)*100000)";
			AssertEquals(worksheet.GetCellText(0, 0), "58509");

			// cell reference sin
			worksheet[0, 1] = 0.625;
			worksheet[0, 2] = "=Math.floor(Math.sin(B1)*100000)";
			worksheet[0, 1] = 1.25;
			AssertEquals(worksheet.GetCellText(0, 2), "94898");
#endif // NO_LONGER_SUPPORTED_V088

			worksheet["A1"] = "=FLOOR(SIN(0.625)*100000)";
			AssertSame(worksheet["A1"], 58509);

			// cell reference sin
			worksheet["B1"] = 0.625;
			worksheet["C1"] = "=FLOOR(SIN(B1)*100000)";
			worksheet["B1"] = 1.25;
			AssertSame(worksheet["C1"], 94898);
		}

		[TestCase]
		void NullReference()
		{
			worksheet["L1"] = "=K1";
			AssertSame(worksheet["L1"], 0);

			worksheet["M1"] = "a";
			worksheet["N1"] = "=M1*2";
			AssertSame(worksheet["N1"], null);
		}

		[TestCase]
		void RangeReference()
		{
			// range contains empty values

			worksheet["A2"] = 10;
			worksheet["C3"] = 20;

			worksheet["D3"] = "=SUM(A2:C3)";

			AssertSame(worksheet["D3"], 30);
		}

		[TestCase]
		void RangeReferenceByName()
		{
			var range = worksheet.DefineNamedRange("range1", "F2:H3");
			worksheet["F2"] = 20;
			worksheet["H3"] = 40;

			worksheet["I3"] = "=SUM(range1)";

			AssertSame(worksheet["I3"], 60);
		}

		[TestCase]
		void ReferenceList()
		{
			worksheet["J1"] = "=A1+B1-SUM(A1:C3)+AVERAGE(D1:H5)";

			var rangeList = worksheet.GetCellFormulaReferenceRanges("J1");
			AssertTrue(rangeList != null);

			AssertEquals(rangeList[0].Position, new RangePosition("A1"));
			AssertEquals(rangeList[1].Position, new RangePosition("B1"));
			AssertEquals(rangeList[2].Position, new RangePosition("A1:C3"));
			AssertEquals(rangeList[3].Position, new RangePosition("D1:H5"));
		}

		[TestCase]
		void CrossWorksheetReference()
		{
			var sheet2 = this.Grid.CreateWorksheet();
			this.Grid.AddWorksheet(sheet2);

			sheet2["A1"] = 10;
			worksheet["K1"] = string.Format("={0}!{1}", sheet2.Name, "A1");
			AssertSame(worksheet["K1"], 10);

			sheet2["A1"] = 20;
			AssertSame(worksheet["K1"], 20);
		}

		[TestCase]
		void CustomFunction_ConstValue()
		{
			// custom function in .NET
			FormulaExtension.CustomFunctions["myfun"] = (cell, args) =>
			{
				return "[" + Convert.ToString(args[0]) + "]";
			};
			worksheet["A2"] = "=myfun(\"abc\")";
			AssertEquals(worksheet["A2"], "[abc]");

			// custom function in .NET
			FormulaExtension.CustomFunctions["add"] = (cell, args) =>
			{
				return (double)args[0] + (double)args[1];
			};

			worksheet["B2"] = "=add(2,3)";
			AssertSame(worksheet["B2"], 5);
		}

		[TestCase]
		void CustomFunction_CellValue()
		{
			// custom function in .NET
			FormulaExtension.CustomFunctions["Div100"] = (cell, args) =>
			{
				// this function requires at least one argument
				if (args.Length < 1) return null;

				// ReoGrid always use double to handle numbers
				if (!(args[0] is double))
				{
					return 0;
				}
				else
				{
					double value = (double)args[0];
					return value / 100;
				}
			};

			// set cell value
			worksheet["C2"] = 100;

			// set value reference to C2
			worksheet["D2"] = "=Div100(C2)";

			// check result
			AssertSame(worksheet["D2"], 1);
		}

		[TestCase]
		void CustomFunction_CellReference()
		{
			FormulaExtension.CustomFunctions["Div200"] = (cell, args) =>
			{
				// this function requires at least one argument
				if (args.Length < 1) return null;

				// the first argument must be address in string
				if (!(args[0] is string))
				{
					return null;
				}

				var addr = (string)args[0];

				if (!CellPosition.IsValidAddress(addr))
				{
					// the address is not valid
					return null;
				}

				// get position
				CellPosition pos = new CellPosition(addr);

				double value = 0;

				// try get value from cell
				if (unvell.ReoGrid.Utility.CellUtility.TryGetNumberData(cell.Worksheet.GetCellData(pos), out value))
				{
					// get value is successful
					return value / 200;
				}
				else
				{
					// cell value might not a number
					return 0;
				}
			};

			// set cell value
			worksheet["E2"] = 400;

			// set address referece to E2 is ADDRESS(2, 5)
			worksheet["F2"] = "=Div200(ADDRESS(2, 5))";

			// check result
			AssertSame(worksheet["F2"], 2);

		}

		[TestCase]
		void CustomFunction_RangeReference()
		{
			FormulaExtension.CustomFunctions["CountEvenNumber"] = (cell, args) =>
			{
				if (args.Length < 1 || !(args[0] is RangePosition))
				{
					return null;
				}

				RangePosition range = (RangePosition)args[0];

				int count = 0;

				// iterate over cells inside a range
				cell.Worksheet.IterateCells(range, (r, c, inCell) =>
					{
						double value;
						if (unvell.ReoGrid.Utility.CellUtility.TryGetNumberData(inCell.Data, out value))
						{
							if ((value % 2) == 0)
							{
								count++;
							}
						}

						// continue iterate
						return true;
					});

				return count;
			};

			worksheet["G2:K3"] = new object[] { 1, 2, 5, 7, 8, 10, 12, 15, 16, 19 };
			worksheet["L2"] = "=CountEvenNumber(G2:K3)";

			AssertSame(worksheet["L2"], 5);
		}

		[TestCase]
		void NameRefercenInCustomFunction()
		{
			worksheet.DefineNamedRange("evenRange", "G2:K3");
			worksheet["evenRange"] = new object[] { 1, 2, 4, 6, 9, 11, 13, 14, 15, 17 };
			worksheet["M2"] = "=CountEvenNumber(evenRange)";
			AssertSame(worksheet["M2"], 4);
		}

		[TestCase]
		void NameProvider()
		{
			FormulaExtension.NameReferenceProvider = (cell, name) =>
				{
					switch (name)
					{
						case "Pi": return Math.PI;
						case "E": return Math.E;
						default: return null;
					}
				};

			worksheet["A3"] = "=Pi";
			AssertSame(worksheet["A3"], Math.PI);

			worksheet["B3"] = "=Pi*2";
			AssertSame(worksheet["B3"], Math.PI * 2);

			worksheet["C3"] = "=Pi+E";
			AssertSame(worksheet["C3"], Math.PI + Math.E);
		}

		[TestCase]
		void NameUsedInFormula()
		{
			worksheet["D3"] = "=Div100(Pi)";
			AssertSame(worksheet["D3"], Math.PI / 100);
		}

		[TestCase]
		void PercentCalc()
		{
			var cell1 = worksheet.Cells["D4"];
			cell1.Data = 200;

			var cell2 = worksheet.Cells["E4"];
			cell2.DataFormat = DataFormat.CellDataFormatFlag.Percent;
			cell2.Data = 5;

			var cell3 = worksheet.Cells["F4"];
			cell3.Formula = "D4*E4";

			AssertSame(cell3.Data, 1000); // 200 * 5 = 1000

			cell2.Data = "5%";
			AssertSame(cell3.Data, 10); // 200 * 5% = 200 * 0.05 = 10
		}

		[TestCase]
		void CrossSheetName()
		{
			worksheet["F4"] = 10;

			// This feature is reserved in 0.8.8,
			// just make sure there is no exceptions happen during parsing formula
			this.Grid.ExceptionHappened += Grid_ExceptionHappened;
			worksheet["G4"] = "=Sheet1!F4";
			this.Grid.ExceptionHappened -= Grid_ExceptionHappened;

			// Reserved
			// AssertSame(worksheet["G4"], 10);
		}

		[TestCase]
		void CrossSheetRange()
		{
			worksheet["H4"] = 20;

			// This feature is reserved in 0.8.8,
			// just make sure there is no exceptions happen during parsing formula
			this.Grid.ExceptionHappened += Grid_ExceptionHappened;
			worksheet["I4"] = "=SUM(Sheet1!F4:H4)";
			this.Grid.ExceptionHappened -= Grid_ExceptionHappened;

			// Reserved
			// AssertSame(worksheet["I4"], 40);
		}

		[TestCase]
		void FunctionNamespaceExtension()
		{
			unvell.ReoGrid.Formula.FormulaExtension.CustomFunctions["funcNsEx"] = (cell, args) =>
			{
				return 1;
			};

			// Function namespace is reserved feature in 0.8.8
			// Just redirect to find the function in global scope
			worksheet["J4"] = "=X.funcNsEx()";

			AssertSame(worksheet["J4"], 1);
		}

		void Grid_ExceptionHappened(object sender, Events.ExceptionHappenEventArgs e)
		{
			AssertTrue(false, "Internal exception happened: " + e.Exception.Message);
		}

#if NO_SUPPORT_V088
		[TestCase]
		public void FreeFormulaEvaluation()
		{
			// free formula does not need '=' prefix
			string formula = "1+2*3";
			var result = worksheet.EvaluateFormula(formula);
			AssertEquals(result, (double)7);

			worksheet["K1"] = "3";
			worksheet["K2"] = "5";
			formula = "K1+K2*2";
			AssertEquals(worksheet.EvaluateFormula(formula), (double)13);

			worksheet["E10"] = "hello";
			var expression = "E10+'world'+'!'.repeat(3)";
			AssertEquals(worksheet.EvaluateFormula(expression), "helloworld!!!");
		}

		[TestCase]
		public void NamedRangeCalculate()
		{
			worksheet.DefineNamedRange("mycell", new ReoGridRange("A1"));
			worksheet["mycell"] = 10;

			worksheet["C3"] = "=mycell+15";

			AssertEquals(worksheet.GetCellText("C3"), "25");
			AssertEquals((double)worksheet.EvaluateFormula("mycell*2"), 20d);
		}
#endif // NO_SUPPORT_V088
	}
}
