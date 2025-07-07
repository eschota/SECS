mergeInto(LibraryManager.library, {
    GetCookie: function(namePtr, bufferPtr, bufferSize) {
        try {
            var name = UTF8ToString(namePtr);
            var cookies = document.cookie.split('; ');
            var value = '';
            for (var i = 0; i < cookies.length; i++) {
                var parts = cookies[i].split('=');
                if (parts[0] === name) {
                    value = decodeURIComponent(parts[1] || '');
                    break;
                }
            }
            // Clamp to buffer size to prevent memory OOB
            if (value.length > bufferSize - 1) {
                console.warn('[WebGLCookie] Truncating cookie "' + name + '" from length ' + value.length + ' to ' + (bufferSize - 1));
                value = value.substring(0, bufferSize - 1);
            }
            stringToUTF8(value, bufferPtr, bufferSize);
        } catch (e) {
            console.error('[WebGLCookie] Error in GetCookie:', e);
        }
    }
}); 