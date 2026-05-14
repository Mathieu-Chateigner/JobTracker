// Auto-dismiss toast after 4s
document.addEventListener('DOMContentLoaded', () => {
    const toast = document.getElementById('toast');
    if (toast) {
        setTimeout(() => {
            toast.style.transition = 'opacity .4s';
            toast.style.opacity = '0';
            setTimeout(() => toast.remove(), 400);
        }, 4000);
    }
});

// Google Places Autocomplete — called by the Maps API callback
function initPlacesAutocomplete() {
    const input = document.getElementById('locationInput');
    if (!input) return;

    const autocomplete = new google.maps.places.Autocomplete(input, {
        // Show cities, addresses, regions — not just establishments
        types: ['geocode', 'establishment'],
        fields: ['formatted_address', 'name', 'address_components']
    });

    autocomplete.addListener('place_changed', () => {
        const place = autocomplete.getPlace();
        if (!place || (!place.formatted_address && !place.name)) return;

        // Prefer a clean city/region format over full street address
        // e.g. "Paris, France" instead of "75001 Paris, France"
        const components = place.address_components || [];
        const city     = components.find(c => c.types.includes('locality'))?.long_name;
        const region   = components.find(c => c.types.includes('administrative_area_level_1'))?.long_name;
        const country  = components.find(c => c.types.includes('country'))?.long_name;

        let location = '';
        if (city && country)        location = `${city}, ${country}`;
        else if (region && country) location = `${region}, ${country}`;
        else                        location = place.formatted_address || place.name || '';

        input.value = location;
    });
}
