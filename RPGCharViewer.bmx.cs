// Lic:
// RPG Character Viewer
// The real work
// 
// 
// 
// (c) Jeroen P. Broks, 
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 
// Please note that some references to data like pictures or audio, do not automatically
// fall under this licenses. Mostly this is noted in the respective files.
// 
// Version: 19.08.13
// EndLic

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

using System.Windows;
using System.Windows.Controls;

using TrickyUnits;
using UseJCR6;

namespace RPGCharViewer {

    class BMXConfig {

        string File => Dirry.C("$AppSupport$/RPGCharViewer.Config.GINI");
        readonly TGINI Data;

        public BMXConfig() {
            if (!System.IO.File.Exists(File)) {
                QuickStream.SaveString(File,"[rem]\nIt's such a perfect day, you just keep me hanging on!\n");
                MessageBox.Show($"Since the file '{File}' which is needed for configuration didn't exist, it has been created!","Notice!", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            Data = GINI.ReadFromFile(File);
        }

        public string this[string key] {
            get => Data.C(key);
            set {
                Data.D(key,value);
                Data.SaveSource(File);
            }
        }

        public string HTML_Swap {
            get {
                if (this["HTML_SWAP"] == "") {
                    this["HTML_SWAP"] = Dirry.C("$AppSupport$/RPGCharView.Swap").Replace("\\","/");
                    MessageBox.Show($"HTML_SWAP was not defined.\nIt's for now been auto-defined on {this["HTML_SWAP"]}!\nIf you want an other directory, please edit {File} (with an editor that is LF only text-file compatible)", "Notice!", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return Dirry.AD(this["HTML_SWAP"]);
            }
        }

    }

    static class BMX { // Let's give BlitzMax, the once so wonderful language a fond honorable adieu, this way.... :-/

        static BMXConfig Config;
        static public Label Copy;
        static public WebBrowser Browser;
        static public MainWindow MWindow;

        static string Mascot => $"{Config.HTML_Swap}/RPGCharViewer.png";

        static string _htmlhead = "";
        static public string htmlhead {
            get {
                if (_htmlhead != "") return _htmlhead;
                var sb = new StringBuilder("<html><head><title>Result RPG Character Viewer</title>");
                sb.Append("\t<style>\n");
                sb.Append("		body    { background-color: #001100; color: #aaffaa; }\n");
                sb.Append("		.error  { background-color: #ff0000; color: #ffff00; }\n");
                sb.Append("		.content{ background-color: #331100; color: #ffbe00; }\n");
                sb.Append("		.shared { background-color: #001100; color: rgb(180,100,255); }\n");
                sb.Append("	</style>\n");
                sb.Append("</head>\n");
                sb.Append("\n\n\n<body>\n");
                _htmlhead = sb.ToString();
                return _htmlhead;
            }
        }
        public const string htmlend = "</body>\n</html>";

        static void WelcomeHTML() {
            try {
                var str = $"{htmlhead}<body>\n <table> \n\t <tr>\n\t\t<td>\n\t\t\t<img src='file://{Mascot}' alt='RPG Character Viewer'>\n\t\t </td>\n\t\t<td> \n\t\t\t <h1>RPGChar Viewer </h1> \n <pre>{MKL.All()}</pre>\n\t\t\t<p >Please throw me a save game file to analyse</td>\n\t\t</tr>\n\t</table>\n<p align = center > &copy Jeroen Petrus Broks {MKL.CYear(2015)}\n</ p >{htmlend}";
                var dir = Config.HTML_Swap;
                if (!File.Exists(Mascot)) {
                    Directory.CreateDirectory(Config.HTML_Swap);
                    var btp = QuickStream.OpenEmbedded("RPGCharViewer.png");
                    var buf = btp.ReadBytes((int)btp.Size);
                    btp.Close();
                    btp = QuickStream.WriteFile(Mascot);
                    btp.WriteBytes(buf);
                    btp.Close();
                }
                var WelcomeFile = $"{dir}/Welcome.html";
                Debug.WriteLine($"Writing \"{WelcomeFile}\"");
                QuickStream.SaveString(WelcomeFile,str);
                Browser.Navigate(WelcomeFile);
            } catch(Exception lul) {
                MessageBox.Show($"{lul.Message}\n\n{lul.StackTrace}", "ERROR!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        static public void Init() {
            // Set Dirry
            Dirry.InitAltDrives();

            // Config
            Config = new BMXConfig();

            // JCR6
            JCR6_lzma.Init();
            JCR6_zlib.Init();
            JCR6_jxsrcca.Init();

            // MKL version information
            MKL.AllWidth = qstr.ToInt(Config["VersionOutLineWidth"]); if (MKL.AllWidth<40) { MKL.AllWidth = 60; Config["VersionOutLineWidth"] = "60"; }
            MKL.Version("RPG Character Viewer - RPGCharViewer.bmx.cs","19.08.13");
            MKL.Lic    ("RPG Character Viewer - RPGCharViewer.bmx.cs","GNU General Public License 3");

            // Write Welcome HTML
            WelcomeHTML();

            // Form the copyright bar
            Copy.Content = $"(c) Jeroen P. Broks {MKL.CYear(2015)}, released under the terms of the GPL 3.0";
            MWindow.Title = $"RPG Character Viewer v{MKL.Newest} - Coded by Jeroen P. Broks";
        }


        static void ThrowUp(string er) {
            MessageBox.Show(er, "ERROR!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        static void WriteLine(QuickStream bt, string line) => bt.WriteString($"{line}\n", true);

        static bool validity(TJCRDIR JCR, QuickStream bt, string E) {
            var ED = JCR.Entries[E.ToUpper()];//Local ED:TJCREntry = TJCREntry(MapValueForKey(JCR.entries, Upper(E)))
            if (ED == null) { WriteLine(bt, "<p class='error'>JCR6 entry " + E + " appears to be illegal.<br>This is very likely an internal error. Please report this immediately!</p>"); return false; }
            if (!JCR6.CompDrivers.ContainsKey(ED.Storage)) { //If Not ListContains(CDRV, ED.Storage)
                WriteLine(bt, "<p class='error'>Required file " + E + " was compressed with an unknown compression algorithm and thus the data inside cannot be retrieved!<br>Supported algorithms in this tool are: <ul>");
                foreach (string D in JCR6.CompDrivers.Keys) WriteLine(bt, "<li>" + D + "</li>");
                WriteLine(bt, "<ul>Either your version of this tool is outdated, or the data inside this entry has been proteced");
                return false;
            }
            return true;
        }

        static bool JCheck(TJCRDIR JCR, QuickStream bt, string E) {
            var ret = JCR.Exists(E);
            if (!ret) { WriteLine(bt, "<p class='error'>Character '" + qstr.StripDir(qstr.ExtractDir(E)) + "' misses the data entry '" + qstr.StripDir(E) + "'. Either this file is incomplete or the engine that wrote this file is outdated!</p>"); }
            return ret;
        }



        static public void Accept(string file) {
            var OutputFile = Dirry.AD($"{Config.HTML_Swap}/Result.html");
            var HMascot = 600;
            var WMascot = 410;
            var wdiv = Browser.Width - WMascot;
            if (file == "") return;
            var jcr = JCR6.Dir(file);
            //if (jcr==null) { ThrowUp(JCR6.JERROR); return; }
            var bt = QuickStream.WriteFile(OutputFile);
            if (bt == null) { ThrowUp("I could not create: " + OutputFile); return; }
            WriteLine(bt, htmlhead);
            WriteLine(bt, $"<table><tr><td><img src='file://{Mascot}' alt='Cute Elf' name='Cute Elf'></td><td>");
            WriteLine(bt, $"<div style='width: {wdiv}pt; height: { HMascot }; overflow-x: auto; overflow-y: auto'>");
            if (jcr == null) {
                WriteLine(bt, $"<p class='error'>*** ERROR ***<br>JCR6 was not able to recognize this file at all.<br>JCR6 threw: {JCR6.JERROR}");
                WriteLine(bt, "Either you were trying to analyse a file that is not related to any game of mine at all, or it wasn't a savegame file, or it was set up for an older game.");
                WriteLine(bt, "Please note that any game released prior to August 2015 will not be picked up by this utility (as they were using a completely different framework, this util was set up for)<br></p>");
                //WriteLine(bt, "<br>RPG Games written by me but NOT supported by this utility are:<ul class='bbc_list' style='list-style-type: lower-greek;'>");
                //For Local G$= EachIn NotSupportedGames
                //  WriteLine bt,"~t~t<li>" + G + "</li>"

                //    Next
                //WriteLine bt,"</ul>"
                //
                //WriteLine bt,"<br>Games either written by me or planned to be written by me which this utility will support are:<ul class='bbc_list' style='list-style-type: lower-greek;'>"

                //For Local G$= EachIn SupportedGames
                //   WriteLine bt,"~t~t<li>" + G + "</li>"

                //Next
                //WriteLine bt,"</ul>"

                //WriteLine bt,"<p class='error'>JCR Reported:<br>" + JCR_Error.ErrorMessage + "</p>"

                bt.Close(); return; //Return closure()
            }
            var dirs = new List<string>();
            //string d = "", ch = "", chd = "";
            Debug.WriteLine($"- Collecting dirs from: {file} ");
            foreach (string f in jcr.Entries.Keys) { //For Local f$= EachIn EntryList(JCR)
                var d = qstr.ExtractDir(f);
                if (dirs.Contains(d)) { Debug.WriteLine($"  = Adding: {d}"); dirs.Add(d); }
            }
            Debug.WriteLine("- Analysing dirs for possible RPG Chars stuff");
            //bool FoundSomething = false;
            var gooddirs = new List<string>();
            bool good = false;
            foreach (string d in dirs) {
                if (jcr.Exists(d + "/LINKS") && jcr.Exists(d + "/PARTY")){
                    good = true;
                    good = good && validity(jcr, bt, d + "/LINKS");
                    good = good && validity(jcr, bt, d + "/PARTY");
                    Debug.WriteLine($"  = Validity checks data > {good}");
                    foreach (TJCREntry che in jcr.Entries.Values) { //        For ch = EachIn EntryList(JCR)
                        var ch = che.Entry;
                        if (qstr.Prefixed(ch.ToUpper(), d + "/CHARACTER/")) {
                            Debug.WriteLine($"  = Checking character: " + qstr.StripDir(qstr.ExtractDir(ch)));
                            good = good && validity(jcr, bt, ch);
                            Debug.WriteLine($"   = Validity after character data file: {qstr.StripDir(ch)} > { good}");
                            var chd = qstr.ExtractDir(ch);
                            good = good && JCheck(jcr, bt, chd + "/LISTS");
                            good = good && JCheck(jcr, bt, chd + "/NAME");
                            good = good && JCheck(jcr, bt, chd + "/POINTS");
                            good = good && JCheck(jcr, bt, chd + "/STATS");
                            good = good && JCheck(jcr, bt, chd + "/STRDATA");
                            Debug.WriteLine($"   = Existence check after character data file: { qstr.StripDir(ch) } > { good}");
                        }
                    }
                    if (good) {
                        gooddirs.Add(d);
                        Debug.WriteLine("  = Approved: " + d);
                    } else {
                        Debug.WriteLine("  = Disapproved: " + d);
                    }
                }
            }
            WriteLine(bt, "<h1>Analysed file: " + qstr.StripDir(file) + "</h1>");
            WriteLine(bt, "Below you can see all data tied to a character, playable or enemy alike (as long as it was in the memory at the moment this data was saved).<br><span class=shared>When the data comes accompanied with a name in this color it means the data is shared with that character, meaning that if the data changes, the data on the linked character will change as well. Multiple share links are possible, and there is no limit to the number of share links a piece of data can contain.</span>");
            RPGCharacter rch;
            RPGCharacter lch;
            //string lchn;
            foreach (string d in gooddirs) {
                WriteLine(bt, "<h2>Folder: " + d + "</h2>");
                RPG.RPGLoad(jcr, d);

                WriteLine(bt, "<h4>Characters in party</h4><ol class=content>");

                foreach (string ch in RPG.RPGParty) {
                    if (ch != "") WriteLine(bt, "<li>" + ch + "</li>");
                }
                WriteLine(bt, "</ol>");

                foreach (string ch in RPG.RPGChars.Keys) {
                    Debug.WriteLine("- Outputting char: " + ch);
                    rch = RPG.GrabChar(ch);
                    WriteLine(bt, "<h4>Character: " + ch + "</h4>");
                    WriteLine(bt, "<h5>Name:</h5><span class=content>" + rch.Name + "</span>");

                    WriteLine(bt, "<h5>Data:</h5><div style='height:200pt; width: 500pt; overflow-y:auto'><table class=content>");
                    foreach (string k in rch.StrData.Keys) {
                        WriteLine(bt, "<tr><td>" + k + "</td><td>=</td><td>" + RPG.GrabChar(ch).GetData( k) + "</td>");

                        foreach (string lchn in RPG.RPGChars.Keys) {
                            lch = RPG.GrabChar(lchn);
                            if (RPG_TMap.MapContains(lch.StrData, k)) {
                                if (RPG_TMap.MapValueForKey(lch.StrData, k) == RPG_TMap.MapValueForKey(rch.StrData, k) && rch != lch) WriteLine(bt, "\t<td class='shared'>" + lchn + "</td>");
                            }
                        }
                        WriteLine(bt, "</tr>");
                    }
                    WriteLine(bt, "</table></div>");

                    Debug.WriteLine("  = Outputting stats");

                    WriteLine(bt, "<h5>Stats:</h5><div style='height:200pt; width: 500pt; overflow-y:auto'><table class=content>");

                    foreach (string k in RPG_TMap.MapKeys(rch.Stats)) { //For Local k$= EachIn MapKeys(rch.Stats)
                        if (k != "") {
                            WriteLine(bt, $"<tr><td>{ k }</td><td>=</td><td>{RPG.GrabChar(ch).Stat( k)}</td>");
                            foreach (string lchn in RPG_TMap.MapKeys(RPG.RPGChars)) { //For lchn = EachIn MapKeys(RPGCHars)
                                lch = RPG.GrabChar(lchn);
                                if (RPG_TMap.MapContains(lch.Stats, k)) {
                                    if (RPG_TMap.MapValueForKey(lch.Stats, k) == RPG_TMap.MapValueForKey(rch.Stats, k) && rch != lch) WriteLine(bt, "\t<td class='shared'>" + lchn + "</td>");
                                }
                            }
                        }
                        WriteLine(bt, "</tr>");
                    }
                    WriteLine(bt, "</table></div>");
                    Debug.WriteLine("  = Outputting points");
                    WriteLine(bt, "<h5>Points:</h5><div style='height:200pt; width: 500pt; overflow-y:auto'><table>");
                    WriteLine(bt, "<tr style='background-color:#000000; color: #ffffff'><td>Key</td><td>Have</td><td>Minimum</td><td>Maximum</td><td>Linked to stat</td></tr>");
                    foreach (string k in RPG_TMap.MapKeys(rch.Points)) { //For Local k$= EachIn MapKeys(rch.Points)
                        if (k != "") {
                            WriteLine(bt, $"<tr class=content><td>{k}</td><td align=right>{ RPG.GrabChar(ch).Point(k).Have}</td><td align=right>{ RPG.GrabChar(ch).Point(k).Minimum }</td><td align=right>{RPG.GrabChar(ch).Point(k).Maximum}</td><td align=center>{RPG.GrabChar(ch).Point( k).MaxCopy}</td>");
                            foreach (string lchn in RPG_TMap.MapKeys(RPG.RPGChars)) { //For lchn = EachIn MapKeys(RPGCHars)
                                lch = RPG.GrabChar(lchn);
                                if (RPG_TMap.MapContains(lch.Points, k)) {
                                    if (RPG_TMap.MapValueForKey(lch.Points, k) == RPG_TMap.MapValueForKey(rch.Points, k) && rch != lch) WriteLine(bt, "\t<td class='shared'>" + lchn + "</td>");
                                }
                            }
                            WriteLine(bt, "</tr>");
                        }
                    }
                    WriteLine(bt, "</table></div>");
                    WriteLine(bt, "<h5>Lists</h5><div style='height:400pt; width: 500pt; overflow-y:auto'><ul>");
                    foreach (string k in RPG_TMap.MapKeys(rch.Lists) ){ //For Local k$= EachIn MapKeys(rch.lists)
                        WriteLine(bt, "\t<li>" + k + "<ol type=i>");
                        foreach (string lchn in RPG_TMap.MapKeys(RPG.RPGChars) ){ //For lchn = EachIn MapKeys(RPGCHars)
                            lch = RPG.GrabChar(lchn);
                            if (RPG_TMap.MapContains(lch.Lists, k)) {
                                if (lch.List(k) == rch.List(k) && rch != lch) WriteLine(bt, "~t<span style='color:rgb(180,100,255)'>This list has been shared with " + lchn + "</span><br>");

                            }
                        }

                        if (rch.List(k).Count == 0) WriteLine(bt, "<span class='error'>This list is empty</span>"); //If Not CountList(rch.list(k)) WriteLine bt,"<span class='error'>This list is empty</span>"
                        else {
                            foreach (string item in rch.List(k)) { //For Local item$= EachIn rch.list(k)
                                WriteLine(bt, "\t\t<li>" + item + "</li>");
                            }
                        }
                        WriteLine(bt, "\t</ol>");
                        WriteLine(bt, "\t</li>");
                    }
                    WriteLine(bt, "</ul></div>");
                }
            }
            bt.Close();
            Browser.Navigate(OutputFile);
                return; // Return closure()
        }
    }
}

// The code below is the original BlitzMax code
// Assuming you are genius enough to get the BlitzMax compiler to work (it requires effort these days, I warn ya), the code below should still work, 
// But as the BlitzMax compiler is is a bad state (I am lucky I still have an old one), I transformed this project, but the code below helps me on 
// several reference issues, you know....

/*
Rem

	RPG Char Viewer for Tricky's RPG Char framework
	This utility provides a full list of all data of all characters found within a savegame created with this framework
	
	
	
	(c) Jeroen P. Broks, 2015, All rights reserved
	
		This program is free software: you can redistribute it and/or modify
		it under the terms of the GNU General Public License as published by
		the Free Software Foundation, either version 3 of the License, or
		(at your option) any later version.
		
		This program is distributed in the hope that it will be useful,
		but WITHOUT ANY WARRANTY; without even the implied warranty of
		MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
		GNU General Public License for more details.
		You should have received a copy of the GNU General Public License
		along with this program.  If not, see <http://www.gnu.org/licenses/>.
		
	Exceptions to the standard GNU license are available with Jeroen's written permission given prior 
	to the project the exceptions are needed for.



Version: 15.08.15

End Rem
Strict
Rem
History
- 15.08.14 - Initial version
End Rem

Framework brl.eventqueue
Import tricky_units.Dirry
Import tricky_units.Bye
Import tricky_units.advdatetime
Import tricky_units.rpgstats
Import tricky_units.prefixsuffix
Import maxgui.drivers
Import jcr6.zlibdriver
Import brl.max2d
Import gale.mgui

?win32
Import "RPGCharViewer.o" ' Icon for Windows
?

Incbin "RPGCharViewer.png"


MKL_Version "RPGCharViewer - RPGCharViewer.bmx","15.08.15"
MKL_Lic     "RPGCharViewer - RPGCharViewer.bmx","GNU - General Public License ver3"

RPG_IgnoreScripts = True ' This untility can never know where to fetch the tied Lua scripts, and frankly it's not needed either.

' Supported, and not supported
Global SupportedGames$[] = ["Star Story (in development)","Realms of Ryromia (planned)","The Fairy Tale REVAMP (planned)"]
Global NotSupportedGames$[] = ["Power of the Rings I","Power of the Rings II","The Fairy Tale (original version)","The Secrets of Dyrt"]

'Global Mascotte:TImage = LoadImage("incbin::RPGCharViewer.png")
Global hmascotte = 590 'ImageHeight(mascotte); Print "Our mascotte is "+hmascotte+" pixel high"
Global wmascotte = 500 'ImageWidth(mascotte); Print "Our mascotte is "+wmascotte+" pixel wide"
'mascotte=Null ' We don't need it any more, so let's not waste any memory on it.

Global CDRV:TList = ListFromArray(ListCompDrivers())

Global JCR:TJCRDir

' INIT
AppTitle = "RPGCharViewer v"+MKL_NewestVersion()+" - Coded by Tricky"
Global Outputdir$ = Dirry("$AppSupport$/Phantasar Productions/Tools/RPGCharViewer")
Global Outputfile$ = Outputdir+"/Result.html"
Global Outputimage$ = Outputdir+"/Picture.png"
?Not MacOS
Const command$="Ctrl"
?MacOS
Const command$="Apple"
?
If Not FileType(Outputdir) 
	Print "Creating: "+Outputdir
	If Not CreateDir(Outputdir,2) FatalError "Could not create: "+Outputdir
	EndIf
Print "Creating: "+Outputfile	
Global BT:TStream = WriteFile(Outputfile) 
If Not BT FatalError "FatalError creating: "+Outputfile
WriteLine bt,"<html><style>body{ background-color: #001100; color: #aaffaa; }</style><body>~n<table>~n~t<tr>~n~t~t<td>~n~t~t~t<img src='Picture.png'>~n~t~t</td>~n~t~t<td>~n~t~t~t<h1>RPGChar Viewer</h1>~n<pre>"+MKL_GetAllversions()+"</pre>~n~t~t~t<p>You can press "+command+"-O to open a file</td>~n~t~t</tr>~n~t</table>~n<p align=center>&copy Jeroen Petrus Broks 2015-"+Year()+"~n</p></body></html>"
CloseFile bt
If Not FileType(OutputImage) 
	Print "Extracting: "+Outputimage
	If Not CopyFile("incbin::RPGCharViewer.png",Outputimage) FatalError "Could not save: "+Outputimage
	EndIf
	
	
'GUI
Global Window:TGadget = CreateWindow(AppTitle,0,0,ClientWidth(Desktop())*.75, ClientHeight(Desktop())*.75,Null,window_titlebar|window_clientcoords|Window_menu|WIndow_Center) '|window_acceptfiles (HTML view appears to be dominant and thus I cannot get this to work properly)
Global WW = ClientWidth(window), WH=ClientHeight(window)
Global HTMLView:TGadget = CreateHTMLView(0,0,WW,WH,window)
Global wscreen = ClientWidth(HTMLView)*.80
Global wdiv = wscreen - wmascotte

RefreshPage

Global Menu:TGadget = CreateMenu("Menu",0,WindowMenu(Window))
CreateMenu "Open Saved File",1001,Menu,KEY_O,MODIFIER_COMMAND
CreateMenu "Save Dump File",1003,Menu,KEY_S,MODIFIER_COMMAND | MODIFIER_SHIFT
CreateMenu "",0,menu
CreateMenu "Copy",1002,Menu,KEY_C,MODIFIER_COMMAND
?Not MacOS
CreateMenu "",0,menu
CreateMenu "Exit",9999,Menu,KEY_X,MODIFIER_COMMAND
?

UpdateWindowMenu Window


' GUI Errorlog
GALE_ConsoleGadget = CreateTextArea(0,0,ww,wh,window); HideGadget GALE_ConsoleGadget
GALE_ExitGadget = GALE_ConsoleGadget
ListAddLast GALEGUI_HideOnError, HTMLView
SetGadgetFont GALE_ConsoleGadget,LookupGuiFont(GUIFONT_MONOSPACED,15)
SetGadgetColor GALE_ConsoleGadget, 10,0,0,1
SetGadgetColor GALE_ConsoleGadget,255,0,0,0



' Functions
Function FatalError(E$)
Notify "Fatal Error!~n~n"+E
Bye
End Function

Function Error(E$)
Notify "Error!~n~n"+E
End Function


Function RefreshPage()
HtmlViewGo HTMLView,Outputfile
End Function

Function closure()
WriteLine bt,"</div>"
WriteLine bt,"</td></tr></table>~n<p align=center>&copy Jeroen Petrus Broks 2015-"+Year()+"~n</p>"
WriteLine bt,"</body>"
WriteLine bt,"</html>"
CloseFile bt
refreshpage
End Function

Function Validity(E$)
Local ED:TJCREntry = TJCREntry(MapValueForKey(JCR.entries,Upper(E)))
If Not ED WriteLine bt,"<p class='error'>JCR6 entry "+E+" appears to be illegal.<br>This is very likely an internal error. Please report this immediately!</p>" Return 
If Not ListContains(CDRV,ED.Storage) 
	WriteLine bt,"<p class='error'>Required file "+E+" was compressed with an unknown compression algorithm and thus the data inside cannot be retrieved!<br>Supported algorithms in this tool are: <ul>"
	For Local D$=EachIn CDRV WriteLine bt,"<li>"+D+"</li>" Next
	WriteLine bt,"<ul>Either your version of this tool is outdated, or the data inside this entry has been proteced"
	Return 
	EndIf
Return 1
End Function

Function JCheck(E$)
Local ret=JCR_Exists(JCR,E)
If Not ret WriteLine bt,"<p class='error'>Character '"+StripDir(ExtractDir(E))+"' misses the data entry '"+StripDir(E)+"'. Either this file is incomplete or the engine that wrote this file is outdated!</p>"
Return ret
End Function


Function Accept(file$)
If Not file Return
jcr:TJCRDir = JCR_Dir(file)
bt = WriteFile(Outputfile)
If Not bt Return error("I could not create: "+Outputfile)
WriteLine bt,"<html><head>"
WriteLine bt,"~t<style>"
WriteLine bt,"~t~tbody    { background-color: #001100; color: #aaffaa; }"
WriteLine bt,"~t~t.error  { background-color: #ff0000; color: #ffff00; }"
WriteLine bt,"~t~t.content{ background-color: #331100; color: #ffbe00; }"
WriteLine bt,"~t~t.shared { background-color: #001100; color: rgb(180,100,255); }"
WriteLine bt,"~t</style>"
WriteLine bt,"</head>"
WriteLine bt,"~n~n~n<body>"
WriteLine bt,"<table><tr><td><img src='Picture.png' alt='Cute Elf' name='Cute Elf'></td><td>"
WriteLine bt,"<div style='width: "+wdiv+"pt; height: "+wmascotte+"; overflow-x: auto; overflow-y: auto'>"
If Not jcr
	WriteLine bt,"<p class='error'>*** ERROR ***<br>JCR6 was not able to recognize this file at all.<br>"
	WriteLine bt,"Either you were trying to analyse a file that is not related to any game of mine at all, or it wasn't a savegame file, or it was set up for an older game."
	WriteLine bt,"Please note that any game released prior to August 2015 will not be picked up by this utility (as they were using a completely different framework, this util was set up for)<br></p>"
	WriteLine bt,"<br>RPG Games written by me but NOT supported by this utility are:<ul class='bbc_list' style='list-style-type: lower-greek;'>"
	For Local G$=EachIn NotSupportedGames
		WriteLine bt,"~t~t<li>"+G+"</li>"
		Next
	WriteLine bt,"</ul>"
	WriteLine bt,"<br>Games either written by me or planned to be written by me which this utility will support are:<ul class='bbc_list' style='list-style-type: lower-greek;'>"
	For Local G$=EachIn SupportedGames
		WriteLine bt,"~t~t<li>"+G+"</li>"
		Next		
	WriteLine bt,"</ul>"
	WriteLine bt,"<p class='error'>JCR Reported:<br>"+JCR_Error.ErrorMessage+"</p>"
	Return closure()
	EndIf
Local dirs:TList = New TList
Local d$,ch$,chd$
Print "- Collecting dirs from: "+File
For Local f$=EachIn EntryList(JCR) 
	d = ExtractDir(f)
	If Not ListContains(dirs,d) Print "  = Adding:"+d; ListAddLast dirs,d
	Next
Print "- Analysing dirs for possible RPG Chars stuff"	
Local FoundSomething
Local gooddirs:TList = New TList
Local Good
For d=EachIn dirs
	If JCR_Exists(JCR,d+"/LINKS") And JCR_Exists(JCR,d+"/PARTY")
		good = True
		good = good And validity(d+"/LINKS")
		good = good And validity(d+"/PARTY")
		Print "  = Validity checks data > "+good
		For ch=EachIn EntryList(JCR)
			If Prefixed(ch,d+"/CHARACTER/") 
				Print "  = Checking character: "+StripDir(ExtractDir(ch))
				good = good And validity(ch)
				Print "   = Validity after character data file: "+StripDir(ch)+" > "+good				
				chd$ = ExtractDir(ch)	
				good = good And JCheck(chd+"/LISTS")
				good = good And JCheck(chd+"/NAME")
				good = good And JCheck(chd+"/POINTS")
				good = good And JCheck(chd+"/STATS")
				good = good And JCheck(chd+"/STRDATA")
				Print "   = Existence check after character data file: "+StripDir(ch)+" > "+good				
				EndIf
			Next
		If good 	
			ListAddLast gooddirs,d
			Print "  = Approved: "+d
			Else
			Print "  = Disapproved: "+d
			EndIf
		EndIf
	Next
	
WriteLine bt,"<h1>Analysed file: "+StripDir(File)+"</h1>"	
WriteLine bt,"Below you can see all data tied to a character, playable or enemy alike (as long as it was in the memory at the moment this data was saved).<br><span class=shared>When the data comes accompanied with a name in this color it means the data is shared with that character, meaning that if the data changes, the data on the linked character will change as well. Multiple share links are possible, and there is no limit to the number of share links a piece of data can contain.</span>"
Local rch:RPGCharacter
Local lch:RPGCharacter
Local lchn$
For d=EachIn gooddirs
	WriteLine bt,"<h2>Folder: "+d+"</h2>"
	RPGLoad JCR,d
	WriteLine bt,"<h4>Characters in party</h4><ol class=content>"
	For ch=EachIn RPGParty
		If ch WriteLine bt,"<li>"+ch+"</li>"
		Next
	WriteLine bt,"</ol>"
	For ch=EachIn MapKeys ( RPGChars )
		Print "- Outputting char: "+ch
		rch = grabchar(ch)
		WriteLine bt,"<h4>Character: "+ch+"</h4>"
		WriteLine bt,"<h5>Name:</h5><span class=content>"+rch.Name+"</span>"
		WriteLine bt,"<h5>Data:</h5><div style='height:200pt; width: 500pt; overflow-y:auto'><table class=content>"
		For Local k$=EachIn MapKeys(rch.strdata)
			WriteString bt,"<tr><td>"+k+"</td><td>=</td><td>"+RPGChar.GetData(ch,k)+"</td>"
			For lchn = EachIn MapKeys(RPGCHars)
				lch = grabchar(lchn)
				If MapContains(lch.StrData,k)
					If MapValueForKey(lch.StrData,k)=MapValueForKey(rch.StrData,k) And rch<>lch WriteLine bt,"~t<td class='shared'>"+lchn+"</td>"
					EndIf
				Next
			WriteLine   bt,"</tr>"
			Next
		WriteLine bt,"</table></div>"	
		Print "  = Outputting stats"
		WriteLine bt,"<h5>Stats:</h5><div style='height:200pt; width: 500pt; overflow-y:auto'><table class=content>"
		For Local k$=EachIn MapKeys(rch.Stats)
			If k 
				WriteLine bt,"<tr><td>"+k+"</td><td>=</td><td>"+rpgchar.stat(ch,k)+"</td>"
				For lchn = EachIn MapKeys(RPGCHars)
					lch = grabchar(lchn)
					If MapContains(lch.stats,k)
						If MapValueForKey(lch.stats,k)=MapValueForKey(rch.Stats,k) And rch<>lch WriteLine bt,"~t<td class='shared'>"+lchn+"</td>"
						EndIf
					Next
				EndIf			
			WriteLine   bt,"</tr>"
			Next	
		WriteLine bt,"</table></div>"
		Print "  = Outputting points"
		WriteLine bt,"<h5>Points:</h5><div style='height:200pt; width: 500pt; overflow-y:auto'><table>"
		WriteLine bt,"<tr style='background-color:#000000; color: #ffffff'><td>Key</td><td>Have</td><td>Minimum</td><td>Maximum</td><td>Linked to stat</td></tr>"
		For Local k$=EachIn MapKeys(rch.Points)
			If k
				WriteString bt,"<tr class=content><td>"+k+"</td><td align=right>"+Rpgchar.points(ch,k).Have+"</td><td align=right>"+Rpgchar.points(ch,k).Minimum+"</td><td align=right>"+Rpgchar.points(ch,k).Maximum+"</td><td align=center>"+Rpgchar.points(ch,k).MaxCopy+"</td>"
				For lchn = EachIn MapKeys(RPGCHars)
					lch = grabchar(lchn)
					If MapContains(lch.points,k)
						If MapValueForKey(lch.points,k)=MapValueForKey(rch.points,k) And rch<>lch WriteString bt,"~t<td class='shared'>"+lchn+"</td>"
						EndIf
					Next
				WriteLine bt,"</tr>"	
				EndIf
			Next
		WriteLine bt,"</table></div>"	
		WriteLine bt,"<h5>Lists</h5><div style='height:400pt; width: 500pt; overflow-y:auto'><ul>";			
		For Local k$=EachIn MapKeys(rch.lists)
			WriteLine bt,"~t<li>"+k+"<ol type=i>"
			For lchn = EachIn MapKeys(RPGCHars)
				lch = grabchar(lchn)
				If MapContains(lch.lists,k)
					If lch.list(k)=rch.list(k) And rch<>lch WriteLine bt,"~t<span style='color:rgb(180,100,255)'>This list has been shared with "+lchn+"</span><br>"
					EndIf
				Next
			If Not CountList(rch.list(k)) WriteLine bt,"<span class='error'>This list is empty</span>"
			For Local item$=EachIn rch.list(k)
				WriteLine bt,"~t~t<li>"+item+"</li>"
				Next
			WriteLine bt,"~t</ol>"	
			WriteLine bt,"~t</li>"			
			Next
		WriteLine bt,"</ul></div>"	
		Next
	Next	
Return closure()	
End Function

Function SaveDump()
Local F$=RequestFile("Please enter a name for the dump file","Packed HTML file:phf",1)
If Not f Return
Local BO:TJCRCreate = JCR_Create(F)
BO.addentry outputfile,"index.html","zlib"
BO.addentry Outputimage,StripDir(outputimage),"zlib"
bo.close
End Function


' Main
Repeat
WaitEvent
Select EventID()
	Case event_menuaction	
		Select EventData()
			Case 1001 Accept RequestFile("Please give me a savegame file")
			Case 1002 GadgetCopy HtmlView
			Case 1003 SaveDump
			Case 9999 Bye
			End Select
	Case Event_windowaccept		
		Accept String(EventExtra())
	Case event_windowclose,event_appterminate	
		Bye
	End Select
Forever
*/

