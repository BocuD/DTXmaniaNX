﻿using System.Runtime.InteropServices;
using DTXMania.Core;
using SharpDX;
using FDK;
using RectangleF = SharpDX.RectangleF;

namespace DTXMania;

public class CActDFPFont : CActivity
{
	// コンストラクタ

	public CActDFPFont()
	{
		STCharacterMap[] st文字領域Array = new STCharacterMap[ 0x5d+2 ];
		STCharacterMap st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域 = st文字領域94;
		st文字領域.ch = ' ';
		st文字領域.rc = new RectangleF( 10, 3, 13, 0x1b );
		st文字領域Array[ 0 ] = st文字領域;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域2 = st文字領域94;
		st文字領域2.ch = '!';
		st文字領域2.rc = new RectangleF( 0x19, 3, 14, 0x1b );
		st文字領域Array[ 1 ] = st文字領域2;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域3 = st文字領域94;
		st文字領域3.ch = '"';
		st文字領域3.rc = new RectangleF( 0x2c, 3, 0x11, 0x1b );
		st文字領域Array[ 2 ] = st文字領域3;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域4 = st文字領域94;
		st文字領域4.ch = '#';
		st文字領域4.rc = new RectangleF( 0x40, 3, 0x18, 0x1b );
		st文字領域Array[ 3 ] = st文字領域4;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域5 = st文字領域94;
		st文字領域5.ch = '$';
		st文字領域5.rc = new RectangleF( 90, 3, 0x15, 0x1b );
		st文字領域Array[ 4 ] = st文字領域5;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域6 = st文字領域94;
		st文字領域6.ch = '%';
		st文字領域6.rc = new RectangleF( 0x71, 3, 0x1b, 0x1b );
		st文字領域Array[ 5 ] = st文字領域6;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域7 = st文字領域94;
		st文字領域7.ch = '&';
		st文字領域7.rc = new RectangleF( 0x8e, 3, 0x18, 0x1b );
		st文字領域Array[ 6 ] = st文字領域7;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域8 = st文字領域94;
		st文字領域8.ch = '\'';
		st文字領域8.rc = new RectangleF( 0xab, 3, 11, 0x1b );
		st文字領域Array[ 7 ] = st文字領域8;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域9 = st文字領域94;
		st文字領域9.ch = '(';
		st文字領域9.rc = new RectangleF( 0xc0, 3, 0x10, 0x1b );
		st文字領域Array[ 8 ] = st文字領域9;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域10 = st文字領域94;
		st文字領域10.ch = ')';
		st文字領域10.rc = new RectangleF( 0xd0, 3, 0x10, 0x1b );
		st文字領域Array[ 9 ] = st文字領域10;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域11 = st文字領域94;
		st文字領域11.ch = '*';
		st文字領域11.rc = new RectangleF( 0xe2, 3, 0x15, 0x1b );
		st文字領域Array[ 10 ] = st文字領域11;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域12 = st文字領域94;
		st文字領域12.ch = '+';
		st文字領域12.rc = new RectangleF( 2, 0x1f, 0x18, 0x1b );
		st文字領域Array[ 11 ] = st文字領域12;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域13 = st文字領域94;
		st文字領域13.ch = ',';
		st文字領域13.rc = new RectangleF( 0x1b, 0x1f, 11, 0x1b );
		st文字領域Array[ 12 ] = st文字領域13;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域14 = st文字領域94;
		st文字領域14.ch = '-';
		st文字領域14.rc = new RectangleF( 0x29, 0x1f, 13, 0x1b );
		st文字領域Array[ 13 ] = st文字領域14;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域15 = st文字領域94;
		st文字領域15.ch = '.';
		st文字領域15.rc = new RectangleF( 0x37, 0x1f, 11, 0x1b );
		st文字領域Array[ 14 ] = st文字領域15;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域16 = st文字領域94;
		st文字領域16.ch = '/';
		st文字領域16.rc = new RectangleF( 0x44, 0x1f, 0x15, 0x1b );
		st文字領域Array[ 15 ] = st文字領域16;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域17 = st文字領域94;
		st文字領域17.ch = '0';
		st文字領域17.rc = new RectangleF( 0x5b, 0x1f, 20, 0x1b );
		st文字領域Array[ 0x10 ] = st文字領域17;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域18 = st文字領域94;
		st文字領域18.ch = '1';
		st文字領域18.rc = new RectangleF( 0x75, 0x1f, 14, 0x1b );
		st文字領域Array[ 0x11 ] = st文字領域18;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域19 = st文字領域94;
		st文字領域19.ch = '2';
		st文字領域19.rc = new RectangleF( 0x86, 0x1f, 0x15, 0x1b );
		st文字領域Array[ 0x12 ] = st文字領域19;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域20 = st文字領域94;
		st文字領域20.ch = '3';
		st文字領域20.rc = new RectangleF( 0x9d, 0x1f, 20, 0x1b );
		st文字領域Array[ 0x13 ] = st文字領域20;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域21 = st文字領域94;
		st文字領域21.ch = '4';
		st文字領域21.rc = new RectangleF( 0xb3, 0x1f, 20, 0x1b );
		st文字領域Array[ 20 ] = st文字領域21;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域22 = st文字領域94;
		st文字領域22.ch = '5';
		st文字領域22.rc = new RectangleF( 0xca, 0x1f, 0x13, 0x1b );
		st文字領域Array[ 0x15 ] = st文字領域22;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域23 = st文字領域94;
		st文字領域23.ch = '6';
		st文字領域23.rc = new RectangleF( 0xe0, 0x1f, 20, 0x1b );
		st文字領域Array[ 0x16 ] = st文字領域23;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域24 = st文字領域94;
		st文字領域24.ch = '7';
		st文字領域24.rc = new RectangleF( 4, 0x3b, 0x13, 0x1b );
		st文字領域Array[ 0x17 ] = st文字領域24;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域25 = st文字領域94;
		st文字領域25.ch = '8';
		st文字領域25.rc = new RectangleF( 0x18, 0x3b, 20, 0x1b );
		st文字領域Array[ 0x18 ] = st文字領域25;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域26 = st文字領域94;
		st文字領域26.ch = '9';
		st文字領域26.rc = new RectangleF( 0x2f, 0x3b, 0x13, 0x1b );
		st文字領域Array[ 0x19 ] = st文字領域26;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域27 = st文字領域94;
		st文字領域27.ch = ':';
		st文字領域27.rc = new RectangleF( 0x44, 0x3b, 12, 0x1b );
		st文字領域Array[ 0x1a ] = st文字領域27;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域28 = st文字領域94;
		st文字領域28.ch = ';';
		st文字領域28.rc = new RectangleF( 0x51, 0x3b, 13, 0x1b );
		st文字領域Array[ 0x1b ] = st文字領域28;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域29 = st文字領域94;
		st文字領域29.ch = '<';
		st文字領域29.rc = new RectangleF( 0x60, 0x3b, 20, 0x1b );
		st文字領域Array[ 0x1c ] = st文字領域29;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域30 = st文字領域94;
		st文字領域30.ch = '=';
		st文字領域30.rc = new RectangleF( 0x74, 0x3b, 0x11, 0x1b );
		st文字領域Array[ 0x1d ] = st文字領域30;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域31 = st文字領域94;
		st文字領域31.ch = '>';
		st文字領域31.rc = new RectangleF( 0x85, 0x3b, 20, 0x1b );
		st文字領域Array[ 30 ] = st文字領域31;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域32 = st文字領域94;
		st文字領域32.ch = '?';
		st文字領域32.rc = new RectangleF( 0x9c, 0x3b, 20, 0x1b );
		st文字領域Array[ 0x1f ] = st文字領域32;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域33 = st文字領域94;
		st文字領域33.ch = 'A';
		st文字領域33.rc = new RectangleF( 0xb1, 0x3b, 0x17, 0x1b );
		st文字領域Array[ 0x20 ] = st文字領域33;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域34 = st文字領域94;
		st文字領域34.ch = 'B';
		st文字領域34.rc = new RectangleF( 0xcb, 0x3b, 0x15, 0x1b );
		st文字領域Array[ 0x21 ] = st文字領域34;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域35 = st文字領域94;
		st文字領域35.ch = 'C';
		st文字領域35.rc = new RectangleF( 0xe3, 0x3b, 0x16, 0x1b );
		st文字領域Array[ 0x22 ] = st文字領域35;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域36 = st文字領域94;
		st文字領域36.ch = 'D';
		st文字領域36.rc = new RectangleF( 2, 0x57, 0x16, 0x1b );
		st文字領域Array[ 0x23 ] = st文字領域36;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域37 = st文字領域94;
		st文字領域37.ch = 'E';
		st文字領域37.rc = new RectangleF( 0x1a, 0x57, 0x16, 0x1b );
		st文字領域Array[ 0x24 ] = st文字領域37;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域38 = st文字領域94;
		st文字領域38.ch = 'F';
		st文字領域38.rc = new RectangleF( 0x30, 0x57, 0x16, 0x1b );
		st文字領域Array[ 0x25 ] = st文字領域38;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域39 = st文字領域94;
		st文字領域39.ch = 'G';
		st文字領域39.rc = new RectangleF( 0x48, 0x57, 0x16, 0x1b );
		st文字領域Array[ 0x26 ] = st文字領域39;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域40 = st文字領域94;
		st文字領域40.ch = 'H';
		st文字領域40.rc = new RectangleF( 0x61, 0x57, 0x18, 0x1b );
		st文字領域Array[ 0x27 ] = st文字領域40;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域41 = st文字領域94;
		st文字領域41.ch = 'I';
		st文字領域41.rc = new RectangleF( 0x7a, 0x57, 13, 0x1b );
		st文字領域Array[ 40 ] = st文字領域41;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域42 = st文字領域94;
		st文字領域42.ch = 'J';
		st文字領域42.rc = new RectangleF( 0x88, 0x57, 20, 0x1b );
		st文字領域Array[ 0x29 ] = st文字領域42;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域43 = st文字領域94;
		st文字領域43.ch = 'K';
		st文字領域43.rc = new RectangleF( 0x9d, 0x57, 0x18, 0x1b );
		st文字領域Array[ 0x2a ] = st文字領域43;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域44 = st文字領域94;
		st文字領域44.ch = 'L';
		st文字領域44.rc = new RectangleF( 0xb7, 0x57, 20, 0x1b );
		st文字領域Array[ 0x2b ] = st文字領域44;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域45 = st文字領域94;
		st文字領域45.ch = 'M';
		st文字領域45.rc = new RectangleF( 0xce, 0x57, 0x1a, 0x1b );
		st文字領域Array[ 0x2c ] = st文字領域45;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域46 = st文字領域94;
		st文字領域46.ch = 'N';
		st文字領域46.rc = new RectangleF( 0xe9, 0x57, 0x17, 0x1b );
		st文字領域Array[ 0x2d ] = st文字領域46;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域47 = st文字領域94;
		st文字領域47.ch = 'O';
		st文字領域47.rc = new RectangleF( 2, 0x73, 0x18, 0x1b );
		st文字領域Array[ 0x2e ] = st文字領域47;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域48 = st文字領域94;
		st文字領域48.ch = 'P';
		st文字領域48.rc = new RectangleF( 0x1c, 0x73, 0x15, 0x1b );
		st文字領域Array[ 0x2f ] = st文字領域48;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域49 = st文字領域94;
		st文字領域49.ch = 'Q';
		st文字領域49.rc = new RectangleF( 0x33, 0x73, 0x17, 0x1b );
		st文字領域Array[ 0x30 ] = st文字領域49;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域50 = st文字領域94;
		st文字領域50.ch = 'R';
		st文字領域50.rc = new RectangleF( 0x4c, 0x73, 0x16, 0x1b );
		st文字領域Array[ 0x31 ] = st文字領域50;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域51 = st文字領域94;
		st文字領域51.ch = 'S';
		st文字領域51.rc = new RectangleF( 100, 0x73, 0x15, 0x1b );
		st文字領域Array[ 50 ] = st文字領域51;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域52 = st文字領域94;
		st文字領域52.ch = 'T';
		st文字領域52.rc = new RectangleF( 0x7c, 0x73, 0x16, 0x1b );
		st文字領域Array[ 0x33 ] = st文字領域52;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域53 = st文字領域94;
		st文字領域53.ch = 'U';
		st文字領域53.rc = new RectangleF( 0x93, 0x73, 0x16, 0x1b );
		st文字領域Array[ 0x34 ] = st文字領域53;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域54 = st文字領域94;
		st文字領域54.ch = 'V';
		st文字領域54.rc = new RectangleF( 0xad, 0x73, 0x16, 0x1b );
		st文字領域Array[ 0x35 ] = st文字領域54;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域55 = st文字領域94;
		st文字領域55.ch = 'W';
		st文字領域55.rc = new RectangleF( 0xc5, 0x73, 0x1a, 0x1b );
		st文字領域Array[ 0x36 ] = st文字領域55;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域56 = st文字領域94;
		st文字領域56.ch = 'X';
		st文字領域56.rc = new RectangleF( 0xe0, 0x73, 0x1a, 0x1b );
		st文字領域Array[ 0x37 ] = st文字領域56;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域57 = st文字領域94;
		st文字領域57.ch = 'Y';
		st文字領域57.rc = new RectangleF( 4, 0x8f, 0x17, 0x1b );
		st文字領域Array[ 0x38 ] = st文字領域57;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域58 = st文字領域94;
		st文字領域58.ch = 'Z';
		st文字領域58.rc = new RectangleF( 0x1b, 0x8f, 0x16, 0x1b );
		st文字領域Array[ 0x39 ] = st文字領域58;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域59 = st文字領域94;
		st文字領域59.ch = '[';
		st文字領域59.rc = new RectangleF( 0x31, 0x8f, 0x11, 0x1b );
		st文字領域Array[ 0x3a ] = st文字領域59;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域60 = st文字領域94;
		st文字領域60.ch = '\\';
		st文字領域60.rc = new RectangleF( 0x42, 0x8f, 0x19, 0x1b );
		st文字領域Array[ 0x3b ] = st文字領域60;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域61 = st文字領域94;
		st文字領域61.ch = ']';
		st文字領域61.rc = new RectangleF( 0x5c, 0x8f, 0x11, 0x1b );
		st文字領域Array[ 60 ] = st文字領域61;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域62 = st文字領域94;
		st文字領域62.ch = '^';
		st文字領域62.rc = new RectangleF( 0x71, 0x8f, 0x10, 0x1b );
		st文字領域Array[ 0x3d ] = st文字領域62;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域63 = st文字領域94;
		st文字領域63.ch = '_';
		st文字領域63.rc = new RectangleF( 0x81, 0x8f, 0x13, 0x1b );
		st文字領域Array[ 0x3e ] = st文字領域63;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域64 = st文字領域94;
		st文字領域64.ch = 'a';
		st文字領域64.rc = new RectangleF( 150, 0x8f, 0x13, 0x1b );
		st文字領域Array[ 0x3f ] = st文字領域64;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域65 = st文字領域94;
		st文字領域65.ch = 'b';
		st文字領域65.rc = new RectangleF( 0xac, 0x8f, 20, 0x1b );
		st文字領域Array[ 0x40 ] = st文字領域65;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域66 = st文字領域94;
		st文字領域66.ch = 'c';
		st文字領域66.rc = new RectangleF( 0xc3, 0x8f, 0x12, 0x1b );
		st文字領域Array[ 0x41 ] = st文字領域66;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域67 = st文字領域94;
		st文字領域67.ch = 'd';
		st文字領域67.rc = new RectangleF( 0xd8, 0x8f, 0x15, 0x1b );
		st文字領域Array[ 0x42 ] = st文字領域67;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域68 = st文字領域94;
		st文字領域68.ch = 'e';
		st文字領域68.rc = new RectangleF( 2, 0xab, 0x13, 0x1b );
		st文字領域Array[ 0x43 ] = st文字領域68;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域69 = st文字領域94;
		st文字領域69.ch = 'f';
		st文字領域69.rc = new RectangleF( 0x17, 0xab, 0x11, 0x1b );
		st文字領域Array[ 0x44 ] = st文字領域69;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域70 = st文字領域94;
		st文字領域70.ch = 'g';
		st文字領域70.rc = new RectangleF( 40, 0xab, 0x15, 0x1b );
		st文字領域Array[ 0x45 ] = st文字領域70;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域71 = st文字領域94;
		st文字領域71.ch = 'h';
		st文字領域71.rc = new RectangleF( 0x3f, 0xab, 20, 0x1b );
		st文字領域Array[ 70 ] = st文字領域71;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域72 = st文字領域94;
		st文字領域72.ch = 'i';
		st文字領域72.rc = new RectangleF( 0x55, 0xab, 13, 0x1b );
		st文字領域Array[ 0x47 ] = st文字領域72;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域73 = st文字領域94;
		st文字領域73.ch = 'j';
		st文字領域73.rc = new RectangleF( 0x62, 0xab, 0x10, 0x1b );
		st文字領域Array[ 0x48 ] = st文字領域73;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域74 = st文字領域94;
		st文字領域74.ch = 'k';
		st文字領域74.rc = new RectangleF( 0x74, 0xab, 20, 0x1b );
		st文字領域Array[ 0x49 ] = st文字領域74;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域75 = st文字領域94;
		st文字領域75.ch = 'l';
		st文字領域75.rc = new RectangleF( 0x8a, 0xab, 13, 0x1b );
		st文字領域Array[ 0x4a ] = st文字領域75;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域76 = st文字領域94;
		st文字領域76.ch = 'm';
		st文字領域76.rc = new RectangleF( 0x98, 0xab, 0x1a, 0x1b );
		st文字領域Array[ 0x4b ] = st文字領域76;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域77 = st文字領域94;
		st文字領域77.ch = 'n';
		st文字領域77.rc = new RectangleF( 0xb5, 0xab, 20, 0x1b );
		st文字領域Array[ 0x4c ] = st文字領域77;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域78 = st文字領域94;
		st文字領域78.ch = 'o';
		st文字領域78.rc = new RectangleF( 0xcc, 0xab, 0x13, 0x1b );
		st文字領域Array[ 0x4d ] = st文字領域78;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域79 = st文字領域94;
		st文字領域79.ch = 'p';
		st文字領域79.rc = new RectangleF( 0xe1, 0xab, 0x15, 0x1b );
		st文字領域Array[ 0x4e ] = st文字領域79;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域80 = st文字領域94;
		st文字領域80.ch = 'q';
		st文字領域80.rc = new RectangleF( 2, 0xc7, 20, 0x1b );
		st文字領域Array[ 0x4f ] = st文字領域80;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域81 = st文字領域94;
		st文字領域81.ch = 'r';
		st文字領域81.rc = new RectangleF( 0x18, 0xc7, 0x12, 0x1b );
		st文字領域Array[ 80 ] = st文字領域81;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域82 = st文字領域94;
		st文字領域82.ch = 's';
		st文字領域82.rc = new RectangleF( 0x2a, 0xc7, 0x13, 0x1b );
		st文字領域Array[ 0x51 ] = st文字領域82;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域83 = st文字領域94;
		st文字領域83.ch = 't';
		st文字領域83.rc = new RectangleF( 0x3f, 0xc7, 0x10, 0x1b );
		st文字領域Array[ 0x52 ] = st文字領域83;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域84 = st文字領域94;
		st文字領域84.ch = 'u';
		st文字領域84.rc = new RectangleF( 80, 0xc7, 20, 0x1b );
		st文字領域Array[ 0x53 ] = st文字領域84;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域85 = st文字領域94;
		st文字領域85.ch = 'v';
		st文字領域85.rc = new RectangleF( 0x68, 0xc7, 20, 0x1b );
		st文字領域Array[ 0x54 ] = st文字領域85;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域86 = st文字領域94;
		st文字領域86.ch = 'w';
		st文字領域86.rc = new RectangleF( 0x7f, 0xc7, 0x1a, 0x1b );
		st文字領域Array[ 0x55 ] = st文字領域86;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域87 = st文字領域94;
		st文字領域87.ch = 'x';
		st文字領域87.rc = new RectangleF( 0x9a, 0xc7, 0x16, 0x1b );
		st文字領域Array[ 0x56 ] = st文字領域87;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域88 = st文字領域94;
		st文字領域88.ch = 'y';
		st文字領域88.rc = new RectangleF( 0xb1, 0xc7, 0x16, 0x1b );
		st文字領域Array[ 0x57 ] = st文字領域88;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域89 = st文字領域94;
		st文字領域89.ch = 'z';
		st文字領域89.rc = new RectangleF( 200, 0xc7, 0x13, 0x1b );
		st文字領域Array[ 0x58 ] = st文字領域89;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域90 = st文字領域94;
		st文字領域90.ch = '{';
		st文字領域90.rc = new RectangleF( 220, 0xc7, 15, 0x1b );
		st文字領域Array[ 0x59 ] = st文字領域90;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域91 = st文字領域94;
		st文字領域91.ch = '|';
		st文字領域91.rc = new RectangleF( 0xeb, 0xc7, 13, 0x1b );
		st文字領域Array[ 90 ] = st文字領域91;
		st文字領域94 = new STCharacterMap();
		STCharacterMap st文字領域92 = st文字領域94;
		st文字領域92.ch = '}';
		st文字領域92.rc = new RectangleF( 1, 0xe3, 15, 0x1b );
		st文字領域Array[ 0x5b ] = st文字領域92;
		STCharacterMap st文字領域93 = new STCharacterMap();
		st文字領域93.ch = '~';
		st文字領域93.rc = new RectangleF( 0x12, 0xe3, 0x12, 0x1b );
		st文字領域Array[ 0x5c ] = st文字領域93;

		st文字領域Array[ 0x5d ] = new STCharacterMap();						// #24954 2011.4.23 yyagi
		st文字領域Array[ 0x5d ].ch = '@';
		st文字領域Array[ 0x5d ].rc = new RectangleF( 38, 227, 28, 28 );
		st文字領域Array[ 0x5e ] = new STCharacterMap();
		st文字領域Array[ 0x5e ].ch = '`';
		st文字領域Array[ 0x5e ].rc = new RectangleF( 69, 226, 14, 29 );

	
		stCharacterRects = st文字領域Array;
	}


	// メソッド

	public int n文字列長dot( string str )
	{
		return n文字列長dot( str, 1f );
	}
	public int n文字列長dot( string str, float fScale )
	{
		if( string.IsNullOrEmpty( str ) )
		{
			return 0;
		}
		int num = 0;
		foreach( char ch in str )
		{
			foreach( STCharacterMap st文字領域 in stCharacterRects )
			{
				if( st文字領域.ch == ch )
				{
					num += (int) ( ( st文字領域.rc.Width - 5 ) * fScale );
					break;
				}
			}
		}
		return num;
	}
	public void t文字列描画( int x, int y, string str )
	{
		t文字列描画( x, y, str, false, 1f );
	}
	public void t文字列描画( int x, int y, string str, bool b強調 )
	{
		t文字列描画( x, y, str, b強調, 1f );
	}
	public void t文字列描画( int x, int y, string str, bool b強調, float fScale )
	{
		if( !bNotActivated && !string.IsNullOrEmpty( str ) )
		{
			CTexture texture = b強調 ? txHighlightCharacterMap : txCharacterMap;
			if( texture != null )
			{
				texture.vcScaleRatio = new Vector3( fScale, fScale, 1f );
				foreach( char ch in str )
				{
					foreach( STCharacterMap st文字領域 in stCharacterRects )
					{
						if( st文字領域.ch == ch )
						{
							System.Drawing.RectangleF rectanglef = new System.Drawing.RectangleF();
							rectanglef.X = st文字領域.rc.X;
							rectanglef.Y = st文字領域.rc.Y;
							rectanglef.Width = st文字領域.rc.Width;
							rectanglef.Height = st文字領域.rc.Height;
							texture.tDraw2D( CDTXMania.app.Device, x, y, rectanglef );
							x += (int) ( ( st文字領域.rc.Width - 5 ) * fScale );
							break;
						}
					}
				}
			}
		}
	}


	// CActivity 実装

	public override void OnManagedCreateResources()
	{
		if( !bNotActivated )
		{
			txCharacterMap = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\Screen font dfp.png" ), false );
			txHighlightCharacterMap = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\Screen font dfp em.png" ), false );
			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		if( !bNotActivated )
		{
			if( txHighlightCharacterMap != null )
			{
				txHighlightCharacterMap.Dispose();
				txHighlightCharacterMap = null;
			}
			if( txCharacterMap != null )
			{
				txCharacterMap.Dispose();
				txCharacterMap = null;
			}
			base.OnManagedReleaseResources();
		}
	}
		

	// Other

	#region [ private ]
	//-----------------
	[StructLayout( LayoutKind.Sequential )]
	internal struct STCharacterMap
	{
		public char ch;
		public RectangleF rc;
	}

	internal readonly STCharacterMap[] stCharacterRects;
	internal CTexture txHighlightCharacterMap;
	internal CTexture txCharacterMap;
	//-----------------
	#endregion
}