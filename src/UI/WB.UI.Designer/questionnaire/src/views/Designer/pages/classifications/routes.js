﻿/*if (!String.prototype.format) {
    String.prototype.format = function() {
        var args = arguments;
        var sprintfRegex = /\{(\d+)\}/g;

        var sprintf = function(match, number) {
            return number in args ? args[number] : match;
        };

        return this.replace(sprintfRegex, sprintf);
    };
}*/

/*var guid = function() {
    function s4() {
        return Math.floor((1 + Math.random()) * 0x10000)
            .toString(16)
            .substring(1);
    }

    return s4() + s4() + s4() + s4() + s4() + s4() + s4() + s4();
};*/

/*Vue.directive('elastic',
    {
        inserted: function(element) {
            var elasticElement = element,
                $elasticElement = $(element),
                initialHeight = initialHeight || $elasticElement.height(),
                delta = parseInt( $elasticElement.css('paddingBottom') ) + parseInt( $elasticElement.css('paddingTop') ) || 0,
                resize = function() {
                    $elasticElement.height(Math.max(20, initialHeight));
                    $elasticElement.height(Math.max(20, elasticElement.scrollHeight - delta));
                };
      
            $elasticElement.on('input change keyup', resize);
            resize();
        }
    });
*/
