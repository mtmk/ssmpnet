#!/bin/perl

use strict;
use warnings;
use TAP::Parser;

my $source = "1..2
OK 1
OK 2
";

{
my $parser = TAP::Parser->new( { source => $source } );
    
    while ( my $result = $parser->next ) {
        print $result->as_string . "\n";
    }
}

    # src/Ssmpnet.Test/bin/Debug/ssmpnett.exe tap
    #
{
my $parser = TAP::Parser->new( { exec => ['src/Ssmpnet.Test/bin/Debug/ssmpnett.exe', 'tap'] } );
    
    while ( my $result = $parser->next ) {
        print $result->as_string . "\n";
    }

    if ($parser->failed) {
	    print "FAILED\n"; 
    }

    for my $e ($parser->parse_errors) {
	    print "PARSE ERROR: $e\n";
    
    }
}

