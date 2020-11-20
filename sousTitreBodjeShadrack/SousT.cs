using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace sousTitreBodjeShadrack
{
    public class SousT
    {
        //LES VARIABLES 
        //State for WSRT & Webvvt reading
        string path = @"C:\Users\bodje\Desktop\homealone2.srt";
        private enum SState { Empty, Adding, Iterating, Comment, Timestamp };
        private enum SSView { Empty, Timestamp, Content }
        public enum SubFormat { NoMatch, SubViewer, MicroDVD };//Pour les autres formats MicroDVD...

        public enum SubtitleNewLineOption { Default, Windows, Unix, MacOLD }

        private static string[] SpaceArray = new string[] { " " }; //Je ne veux pas continuer à les recréer (les espaces ...)
        private static string[] NewLineArray = new string[] { "\n" }; 
        private static string[] CommaArray = new string[] { "," };
        private static string[] CloseSquigArray = new string[] { "}" };

        public SubtitleNewLineOption subtitleNewLineOption = SubtitleNewLineOption.Default;

        //Pour les fonctions qui nécessitent un fps par défaut car je projette de le rendre visuel en video 
        //nous allons donner la definition d'un fps c'est Frames Par Seconde : Unité qui définit le nombre d'images qui est affiché sur un écran d'ordinateur.
        //Pourquoi float, parce que c'est le type des nombres à virgul
        public float specFPSRead = 23.976f;//float de lecture
        public float specFPSWrite = 23.976f;//float pour ecriture

        public SubFormat DotSubSave = SubFormat.SubViewer;

        public Encoding EncodingRead = Encoding.Default;
        //Sous-format interne pour permettre une conversion facile
        private class SubtitleEntry//Constructor
        {
            public DateTime startTime { get; set; }
            public DateTime endTime { get; set; }
            public string content { get; set; }
            public SubtitleEntry(DateTime sTime, DateTime eTime, string text)//la fonction permet de prendre le temps de debut et de fin et le texte des sous titre
            {
                startTime = sTime;
                endTime = eTime;
                content = text;
            }
        }

        List<SubtitleEntry> subTitleLocal;//sous titre locale selon le OS ou si on peut dire selon ce que l'ordi peut supporter

        //Dictionarty /HashMap whatever
        Dictionary<SubtitleNewLineOption, string> nlDict = new Dictionary<SubtitleNewLineOption, string>();

        public SousT()
        {
            nlDict.Add(SubtitleNewLineOption.MacOLD, "\r");
            nlDict.Add(SubtitleNewLineOption.Unix, "\n");
            nlDict.Add(SubtitleNewLineOption.Windows, "\r\n");
        }

        //ON PASSE A LA LECTURE DES DIFFERENTS FORMATS QUE E SOIT SRT OU AUTRE 

        /// <summary>//Le summary c'est en gros ce que je vais faire dans ce qui suit c'est comme un commentaire mais pour cet objet ou ce bloc de code
        /// je Convertit un sous-titre srt dans le format de sous-titre local
        /// </summary>
        /// <param name="path">Input path for the subtitle</param>
        private void ReadSRT(string path)
        {
            string raw = File.ReadAllText(path, EncodingRead);//lisons les textes ou string du fichier
            raw = Regex.Replace(raw, @"<[^>]*>", "");
            string[] split = Regex.Split(raw, @"\n\n[0-9]+\n"); //Chaque entrée peut être séparée comme ceci, un sous-titre ne peut pas contenir une ligne vide suivie d'une ligne ne contenant qu'un nombre décimal séparément
            //Le premier cas est un peu différent car il a une ligne supplémentaire ou peut-être indésirable quand on voit les fichiers qu'on nous donnes
            string case1 = split[0].TrimStart();//TrimStart()Supprime tous les caractères correspondant à un espace blanc au début de la chaîne actuelle.
            string[] splitc1 = case1.Split(new string[] { "\n" }, StringSplitOptions.None);

            string[] time = Regex.Split(splitc1[1], " *--> *");           //Peut ou non avoir un espace ou plus de 2 tirets dans le temps contenu dans les fichiers srt

            DateTime beginTime;
            DateTime endTime;

            beginTime = DateTime.ParseExact(time[0], "HH:mm:ss,fff", CultureInfo.InvariantCulture);
            endTime = DateTime.ParseExact(time[1], "HH:mm:ss,fff", CultureInfo.InvariantCulture);

            string tmp = splitc1[2];
            foreach (string text in splitc1.Skip(3))
            {
                tmp = tmp + "\n" + text;
            }

            subTitleLocal.Add(new SubtitleEntry(beginTime, endTime, tmp));
            //Boucle principale
            foreach (string sub in split.Skip(1))
            {
                string[] splitc2 = sub.Split(new string[] { "\n" }, StringSplitOptions.None);

                string[] time2 = Regex.Split(splitc2[0], " *--> *");           //Peut ou non avoir un espace ou plus de 2 tirets
                DateTime beginTime2;
                DateTime endTime2;
                beginTime2 = DateTime.ParseExact(time2[0], "HH:mm:ss,fff", CultureInfo.InvariantCulture);
                endTime2 = DateTime.ParseExact(time2[1], "HH:mm:ss,fff", CultureInfo.InvariantCulture);

                string tmp2 = splitc2[1].TrimEnd();//Supp toutes les occurences de sorte à ce qu'il ne reste pas de doublure ou doublon
                foreach (string text in splitc2.Skip(2))
                {
                    tmp2 = tmp2 + "\n" + text.TrimEnd();
                }

                subTitleLocal.Add(new SubtitleEntry(beginTime2, endTime2, tmp2));
            }
        }



       

        /// <summary>
        /// Convertit un sous-titre wsrt au format de sous-titre local
        /// Ancien, on peut utilisez plutôt ReadWSRT2
        /// </summary>
        /// <param name="path">Input path for the subtitle</param>
        private void ReadWSRT(string path)
        {
            //Très similaire à ReadSRT, mais supprime les ajouts 
            
            string raw = File.ReadAllText(path, Encoding.Default);
            raw = raw.Replace("\r\n", "\n");
            string[] split = Regex.Split(raw, @"\n\n[0-9]+\n"); //Chaque entrée peut être séparée comme ceci, un sous-titre ne peut pas contenir une ligne vide suivie d'une ligne ne contenant qu'un nombre décimal séparément
            //Le premier cas est un peu différent car il a une ligne supplémentaire ou peut-être indésirable quand on voit les fichiers qu'on nous donnes
            string case1 = split[0].TrimStart();
            string[] splitc1 = case1.Split(NewLineArray, StringSplitOptions.None);

            string[] time = Regex.Split(splitc1[1], " *--> *");           //Peut ou non avoir un espace ou plus de 2 tirets

            DateTime beginTime;
            DateTime endTime;

            beginTime = DateTime.ParseExact(time[0], "HH:mm:ss.fff", CultureInfo.InvariantCulture);
            endTime = DateTime.ParseExact(time[1], "HH:mm:ss.fff", CultureInfo.InvariantCulture);

            string tmp = splitc1[2];
            foreach (string text in splitc1.Skip(3))
            {
                tmp = tmp + "\n" + text;
            }
            tmp = Regex.Replace(tmp, @"</*[0-9]+>", "");
            subTitleLocal.Add(new SubtitleEntry(beginTime, endTime, tmp));
            //Boucle principale
            foreach (string sub in split.Skip(1))
            {
                string[] splitc2 = sub.Split(NewLineArray, StringSplitOptions.None);

                string[] time2 = Regex.Split(splitc2[0], " *--> *");           //Peut ou non avoir un espace ou plus de 2 tirets
                DateTime beginTime2;
                DateTime endTime2;
                beginTime2 = DateTime.ParseExact(time[0].Substring(0, 12), "HH:mm:ss.fff", CultureInfo.InvariantCulture);
                endTime2 = DateTime.ParseExact(time[1].Substring(0, 12), "HH:mm:ss.fff", CultureInfo.InvariantCulture);

                string tmp2 = splitc2[1].TrimEnd(); //Supp toutes les occurences de sorte à ce qu'il ne reste pas de doublure ou doublon
                foreach (string text in splitc2.Skip(2))
                {
                    tmp2 = tmp2 + "\n" + text.TrimEnd();
                }
                tmp2 = Regex.Replace(tmp2, @"</*[0-9]+>", "");
                subTitleLocal.Add(new SubtitleEntry(beginTime2, endTime2, tmp2));
            }

        }

        /// <summary>
        /// Lire un fichier de sous-titres MicroDVD
        /// </summary>
        /// <param name="path">Chemin d'accès au sous-titre à lire</param>
        private void ReadMicroDVD(string path)
        {
            //\d+\.\d+
            DateTime startTime;
            DateTime endTime;
            Regex regexSplit = new Regex(@"(?<=\})");
            Regex removeMeta = new Regex(@"\{[^}]*\}");
            string raw = File.ReadAllText(path, EncodingRead);
            float fps;
            using (StringReader mDVD = new StringReader(raw))
            {
                // Premier cas
                string line = mDVD.ReadLine(); // Les premières options de cas pour le framerate de la vidéo doivent être gérées
                string beginFrameStr;   // Chaîne de cadres pour l'heure de début
                string endFrameStr;   // Chaîne de cadres pour l'heure de fin
                if (!line.StartsWith("{DE"))
                {

                    string[] splitFirst = regexSplit.Split(line, 3);
                    string contentFirst = splitFirst[2].Replace("[", "").Replace("]", "").Replace(",", ".");

                    if (!float.TryParse(contentFirst, out fps))
                    {
                        fps = specFPSRead;
                        beginFrameStr = splitFirst[0].Substring(1, splitFirst[0].Length - 2);
                        endFrameStr = splitFirst[1].Substring(1, splitFirst[1].Length - 2);
                        startTime = framesToDateTime(int.Parse(beginFrameStr), fps);
                        endTime = framesToDateTime(int.Parse(endFrameStr), fps);
                        string content = removeMeta.Replace(splitFirst[2], "").Replace("|", "\n");
                        subTitleLocal.Add(new SubtitleEntry(startTime, endTime, content));
                    }
                }
                else
                {
                    fps = specFPSRead;
                }
                while ((line = mDVD.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line == "" || line.StartsWith("{DE")) continue;
                    string[] split = regexSplit.Split(line, 3);
                    // Chaîne de cadres pour l'heure de fin
                    beginFrameStr = split[0].Substring(1, split[0].Length - 2);
                    endFrameStr = split[1].Substring(1, split[1].Length - 2);
                    // Analyse en datetime
                    startTime = framesToDateTime(int.Parse(beginFrameStr), fps);
                    endTime = framesToDateTime(int.Parse(endFrameStr), fps);
                    // Supprimer le balisage et ajouter des nouvelles lignes
                    string content = removeMeta.Replace(split[2], "").Replace("|", "\n");
                    subTitleLocal.Add(new SubtitleEntry(startTime, endTime, content));
                }
            }
        }
        /*
        /// <summary>
        /// Gère l'analyse du type de format .sub (microdvd, subviewer)
        /// </summary>
        /// <param name="path">Chemin d'accès au sous-titre à lire</param>
        private void ReadSub(string path)
        {
            string head;
            SubFormat format = SubFormat.NoMatch;
            using (StreamReader file = new StreamReader(path))
            {

                head = file.ReadLine();
                if (head.StartsWith("[")) format = SubFormat.SubViewer;
                else if (head.StartsWith("{")) format = SubFormat.MicroDVD;
            }
            switch (format)
            {
                case (SubFormat.SubViewer):
                    ReadSubViewer(path);
                    break;
                case (SubFormat.MicroDVD):
                    ReadMicroDVD(path);
                    break;
                default:
                    break;
            }
        }
        */

        //----------------------------------------------------------------------------------------------
   
        /// <summary>
        /// Supprimez les heures de début du dupicale et rejoignez-en une
        /// Suppose que les sous-marins sont triés
        /// Extrait et modifié à partir du site de developpement shad hihihi http://stackoverflow.com/questions/14918668/find-duplicates-and-merge-items-in-a-list
        /// </summary>
        private void JoinSameStart()
        {
            for (int i = 0; i < subTitleLocal.Count - 1; i++)
            {
                var item = subTitleLocal[i];
                for (int j = i + 1; j < subTitleLocal.Count;)
                {
                    var anotherItem = subTitleLocal[j];
                    if (item.startTime > anotherItem.startTime) break; //Aucun point continue car la liste est triée
                    if (item.startTime.Equals(anotherItem.startTime))
                    {
                        //Nous allons joignons simplement à la list et espérons qu'ils étaient dans le bon ordre
                        //vérifier s'il y a decalage et ordonner par cela
                        item.content = item.content + "\n" + anotherItem.content;
                        subTitleLocal.RemoveAt(j);//Supprime l'element à partir de l'element selectionné J
                    }
                    else
                        j++;
                }
            }
        }

        /// <summary>
        /// Analyse une mesure temporelle soit 12h31m2s44ms
        /// </summary>
        /// <param name="metric">The metric string</param> deocumentation de docs microsoft La <param> balise doit être utilisée dans le commentaire d’une déclaration de méthode pour décrire l’un des paramètres de la méthode. Pour documenter plusieurs paramètres, utilisez plusieurs <param> balises.
        /// <returns>L'équivilent datetime</returns>
        private DateTime ParseTimeMetric(string metric)
        {
            DateTime time = new DateTime();
            Regex rg = new Regex(@"([0-9.]+)([a-z]+)");
            MatchCollection mtchs = rg.Matches(metric);
            foreach (Match match in mtchs)
            {
                float st = float.Parse(match.Groups[1].Value);
                switch (match.Groups[2].Value)
                {
                    case ("h"):
                        time = time.AddHours(st);
                        break;
                    case ("m"):
                        time = time.AddMinutes(st);
                        break;
                    case ("s"):
                        time = time.AddSeconds(st);
                        break;
                    case ("ms"):
                        time = time.AddMilliseconds(st);
                        break;
                }
            }
            return time;
        }
        /// <summary>
        /// Analyse une mesure temporelle soit 12h31m2s44ms
        /// </summary>
        /// <param name="metric">The metric string</param>
        /// <returns>L'équivalent TimeSpan</returns>
        private TimeSpan ParseTimeMetricAsTimeSpan(string metric)
        {
            TimeSpan time = new TimeSpan();
            Regex rg = new Regex(@"([0-9.]+)([a-z]+)");
            MatchCollection mtchs = rg.Matches(metric);
            foreach (Match match in mtchs)
            {
                float st = float.Parse(match.Groups[1].Value);
                switch (match.Groups[2].Value)
                {
                    case ("h"):
                        time += TimeSpan.FromHours(st);//retourne TimeSpan specidifié en heure avec la mesure en millisecondes pres
                        break;
                    case ("m"):
                        time += TimeSpan.FromMinutes(st);
                        break;
                    case ("s"):
                        time += TimeSpan.FromSeconds(st);
                        break;
                    case ("ms"):
                        time += TimeSpan.FromMilliseconds(st);//retourne un nombre specifié de milliseconde
                        break;
                }
            }
            return time;
        }

        /// <summary>
        /// Analyse une mesure temporelle soit 12h31m2s44ms
        /// </summary>
        /// <param name="metric">The metric string</param>
        /// <returns>equivalent TimeSpan</returns>
        private TimeSpan ParseTimeMetricTimeSpan(string metric)
        {
            TimeSpan time = TimeSpan.Zero;
            Regex rg = new Regex(@"([0-9.]+)([a-z]+)");
            MatchCollection mtchs = rg.Matches(metric);
            foreach (Match match in mtchs)
            {
                float st = float.Parse(match.Groups[1].Value);
                switch (match.Groups[2].Value)
                {
                    case ("h"):
                        time = time.Add(TimeSpan.FromHours(st));
                        break;
                    case ("m"):
                        time = time.Add(TimeSpan.FromMinutes(st));
                        break;
                    case ("s"):
                        time = time.Add(TimeSpan.FromSeconds(st));
                        break;
                    case ("ms"):
                        time = time.Add(TimeSpan.FromMilliseconds(st));
                        break;
                }
            }
            return time;
        }

        /// <summary>
        /// Convertit un int représentant des images en un objet datetime
        /// </summary>
        /// <param name="frames">Le nombre de frames</param>
        /// <param name="fps">Les frames par seconde</param>
        /// <returns>L'objet datetime créé</returns>
        private DateTime framesToDateTime(int frames, float fps)
        {
            DateTime dt = new DateTime();
            dt = dt.AddSeconds(frames / fps);
            return dt;
        }

        /// <summary>
        /// Ajoute le timemetric fourni à toutes les entrées de la liste de sous-titres
        /// </summary>
        /// <param name="timeMetric">Une métrique temporelle pour ajuster le sous-titre</param>
        public void AdjustTimingLocalAdd(string timeMetric)
        {
            TimeSpan ts = ParseTimeMetricTimeSpan(timeMetric);
            foreach (SubtitleEntry entry in subTitleLocal)
            {
                entry.startTime = entry.startTime.Add(ts);
                entry.endTime = entry.endTime.Add(ts);
            }
        }

        /// <summary>
        /// Soustraction de le timemetric fourni à toutes les entrées de la liste de sous-titres
        /// </summary>
        /// <param name="timeMetric">Une métrique temporelle pour ajuster le sous-titre</param>
        public void AdjustTimingLocalSub(string timeMetric)
        {
            TimeSpan ts = ParseTimeMetricTimeSpan(timeMetric);
            foreach (SubtitleEntry entry in subTitleLocal)
            {
                DateTime sTNew = entry.startTime.Subtract(ts);
                DateTime eTNew = entry.endTime.Subtract(ts);
                if (sTNew.DayOfYear == entry.startTime.DayOfYear) //Besoin de vérifier si le temsp de start a été insuffisant
                {
                    entry.startTime = sTNew;
                }
                else //sinon si il a débordé
                {
                    entry.startTime = new DateTime(entry.startTime.Year, entry.startTime.Month, entry.startTime.Day, 0, 0, 0, 0, entry.startTime.Kind);
                }

                if (eTNew.DayOfYear == entry.endTime.DayOfYear) // Besoin de vérifier si le temps de fin est insuffisant
                {
                    entry.endTime = eTNew;
                }
                else //sinon s'il a débordé
                {
                    entry.endTime = new DateTime(entry.endTime.Year, entry.endTime.Month, entry.endTime.Day, 0, 0, 0, 0, entry.endTime.Kind);
                }

            }
        }

        /// <summary>
        /// Gets the newline type
        /// </summary>
        /// <param name="defaultValue">The default newline value if its not set</param>
        /// <returns>The newline option</returns>
        private string GetNewlineType(string defaultValue)
        {
            if (subtitleNewLineOption != SubtitleNewLineOption.Default)
            {
                return nlDict[subtitleNewLineOption];
            }
            return defaultValue;
        }
        /*
        /// <summary>
        /// Read a subtitle from the specified input path / extension
        /// </summary>
        /// <param name="input">Path to the subtitle</param>
        /// <returns>A boolean representing the success of the operation</returns>
        public bool ReadSubtitle(string input)
        {
            subTitleLocal = new List<SubtitleEntry>();
            string extensionInput = Path.GetExtension(input).ToLower();
            switch (extensionInput) //Read file
            {
                case (".ass"):
                case (".ssa"):
                    ReadASS(input);
                    break;
                case (".dfxp"):
                case (".ttml"):
                    ReadDFXP(input);
                    break;
                case (".mpl"):
                    ReadMPlayer(input);
                    break;
                case (".sub"):
                    ReadSub(input);
                    break;
                case (".srt"):
                    ReadSRT(input);
                    break;
                case (".vtt"):
                    ReadWebVTT(input);
                    break;
                case (".wsrt"):
                    ReadWSRT2(input);
                    break;
                default:
                    Console.WriteLine("Invalid read file format");
                    return false;
            }
            return true;
        }
        /// <summary>
        /// Writes a subtitle to the specified output path / extension
        /// </summary>
        /// <param name="output">Output path location with file extension</param>
        /// <returns>A boolean representing the success of the operation</returns>
        public bool WriteSubtitle(string output)
        {
            string extensionOutput = Path.GetExtension(output).ToLower();

            switch (extensionOutput) //Write to file
            {
                case (".ass"):
                case (".ssa"):
                    WriteASS(output);
                    break;
                case (".dfxp"):
                case (".ttml"):
                    WriteDFXP(output);
                    break;
                case (".mpl"):
                    WriteMPlayer(output);
                    break;
                case (".sub"):
                    if (DotSubSave == SubFormat.MicroDVD)
                    {
                        WriteMircoDVD(output);
                    }
                    else
                    {
                        WriteSubviewer(output);
                    }
                    break;
                case (".srt"):
                    WriteSRT(output);
                    break;
                case (".vtt"):
                    WriteWebVTT(output);
                    break;
                case (".wsrt"):
                    WriteWSRT(output);
                    break;
                default:
                    Console.WriteLine("Invalid write file format");
                    return false;
            }
            return true;
        }
        */

        /*-----------------------------------------------------------------Partie pour lire et convertir les sous titre en fonction de leur extension-----------------------------*/

        /// <summary>
        /// Lire un sous-titre à partir du chemin / extension d'entrée spécifié
        /// </summary>
        /// <param name="input">Chemin vers le fichié sous-titre</param>
        /// <returns>Un booléen représentant le succès de l'opération</returns>
        public bool ReadSubtitle(string input)
        {
            subTitleLocal = new List<SubtitleEntry>();
            string extensionInput = path.GetExtension(input).ToLower();
            switch (extensionInput) //Read file
            {
                /*case (".ass"):
                case (".ssa"):
                    ReadASS(input);
                    break;
                case (".dfxp"):
                case (".ttml"):
                    ReadDFXP(input);
                    break;
                case (".mpl"):
                    ReadMPlayer(input);
                    break;*/
                case (".srt"):
                    ReadSRT(input);
                    break;
                case (".wsrt"):
                    ReadSRT(input);
                    break;
                default:
                    Console.WriteLine("Invalid read file format");
                    return false;
            }
            return true;
        }
       
        /// <summary>
        /// Convertir un sous-titre, prendre en charge spécifié par l'extension de fichier d'entrée et de sortie
        /// ASS/SSA DFXP/TTML, SUB, SRT, WSRT, VTT;
        /// </summary>
        /// <param name="input">Le chemin d'accès au sous-titre à convertir</param>
        /// <param name="output">Le chemin d'accès à l'emplacement à enregistrer et le nom / type de fichier à convertir</param>
        /// <returns>Un booléen représentant le succès de l'opération</returns>
        public bool ConvertSubtitle(string input, string output, object timeShift)
        {
            return ConvertSubtitle(input, output, "");
        }
        /// <summary>
        /// Convertir un sous-titre, prend en charge spécifié par l'extension de fichier d'entrée et de sortie
        /// ASS/SSA DFXP/TTML, SUB, SRT, WSRT, VTT;
        /// </summary>
        /// <param name="input">Le chemin d'accès au sous-titre à convertir</param>
        /// <param name="output">Le chemin d'accès à l'emplacement à enregistrer et le nom / type de fichier à convertir</param>
        /// <param name="timeshift"> Le temps de décaler le sous-titre</param>
        /// <returns>Un booléen représentant le succès de l'opération</returns>
        /// 
        
        public bool ConvertSubtitle(string input, string output, string timeshift)
        {

            if (!ReadSubtitle(input)) return false; 
            if (!timeshift.Equals(""))//Adjust time
            {
                if (timeshift[0] == '-') AdjustTimingLocalSub(timeshift);
                else AdjustTimingLocalAdd(timeshift);
            }
            /*if (!WriteSubtitle(output)) return false;
            return true;*/
        }
        
    }


}

