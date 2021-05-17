IF EXISTS t:WavModulate
    delete >NIL: t:WavModulate all
ENDIF
makedir t:WavModulate 
copy >NIL: Source/#?.d t:WavModulate all
echo ""
echo "*E[33mCompiling...*E[0m"
echo ""
Draco/c/Draco t:WavModulate/Main.d t:WavModulate/Modulating.d t:WavModulate/File.d t:WavModulate/Parsing.d t:WavModulate/Timing.d
echo ""
echo "*E[33mLinking...*E[0m"
echo ""
Draco/c/Blink with blink.args
delete >NIL: t:WavModulate all
echo ""
echo "*E[33mDone*E[0m - executable built to *E[32mram:WavModulate*E[0m"
echo ""
