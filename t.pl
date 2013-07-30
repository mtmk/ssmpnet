#!/bin/perl
use strict;
use warnings;
use TAP::Parser;

my $total_failed = 0;

for my $t (
['src/Ssmpnet.Test/bin/Debug/ssmpnett.exe', 'tap'],
['src/Ssmpnet.ResilienceTest/bin/Debug/Ssmpnet.ResilienceTest.exe', ''],
['src/Ssmpnet.LoadTest/bin/Debug/Ssmpnet.LoadTest.exe', ''],
['src/Ssmpnet.LoadTest.Netmq/bin/Debug/Ssmpnet.LoadTest.Netmq.exe', ''],
){
	print "\n  _______________________________________\n";
	print "  Running: " . $t->[0] . "\n\n";

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

