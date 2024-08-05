grammar Chess;

parse: move EOF;

move: castling | enPassant | standardMove;

standardMove: piece? disambiguation? capture? square (promotion)? checkOrMate?;

piece: 'K' | 'Q' | 'R' | 'B' | 'N';

disambiguation: file | rank | file rank;

capture: 'x';

square: file rank;

// Lexer

file: 'a' | 'b' | 'c' | 'd' | 'e' | 'f' | 'g' | 'h';
rank: '1' | '2' | '3' | '4' | '5' | '6' | '7' | '8';

promotion: '=' piece;

checkOrMate: '+' | '#';

castling: 'O-O' | 'O-O-O';

enPassant: square 'e.p.';

WS: [ \t\r\n]+ -> skip;
