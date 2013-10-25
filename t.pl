#!/bin/perl

use strict;
use warnings;
use TAP::Parser;

my $total_failed = 0;
my $config = 'Debug';

for my $t (
#["src/Ssmpnet.Test/bin/$config/ssmpnett.exe", 'tap'],
["src/Ssmpnet.ResilienceTest/bin/$config/Ssmpnet.ResilienceTest.exe", 'multi-sub'],
["src/Ssmpnet.ResilienceTest/bin/$config/Ssmpnet.ResilienceTest.exe", 'blink-sub'],
["src/Ssmpnet.ResilienceTest/bin/$config/Ssmpnet.ResilienceTest.exe", 'blink-pub'],
["src/Ssmpnet.LoadTest/bin/$config/Ssmpnet.LoadTest.exe", ''],
#["src/Ssmpnet.LoadTest.Netmq/bin/$config/Ssmpnet.LoadTest.Netmq.exe", ''],
){

	if ($ARGV[0]) {
		next if ($t->[0] !~ /$ARGV[0]/i and $t->[1] !~ /$ARGV[0]/i);
	}

	print "\n  _______________________________________\n";
	print "  Running: " . $t->[0] . " " . $t->[1] . "\n\n";

    my $parser = TAP::Parser->new({
		    exec => $t
	    });
    
    while ( my $result = $parser->next ) {
        print "  " . $result->as_string . "\n";
    }

    print "\n";

    my $failed = $parser->failed;

    for my $e ($parser->parse_errors) {
	    print "  PARSE ERROR: $e\n";
            $failed = 1;
    }

    if ($failed) {
	    $total_failed++;
	    print "  FAILED\n"; 
    } else {
	    print "  SUCCESS\n"; 
    }

}


print "\n_______________________________________\n";
if ($total_failed) {
	print "RESULT: FAILED\n\n";
} else {
	print "RESULT: SUCCESS\n\n";
}

