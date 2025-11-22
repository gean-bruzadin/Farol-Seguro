
function toggleDropdown() {
    const menu = document.getElementById('dropdownMenu');
    const isVisible = menu.style.opacity === '1';

    if (isVisible) {
        menu.style.opacity = '0';
        menu.style.maxHeight = '0';
    } else {
        menu.style.display = 'block';
        setTimeout(() => {
            menu.style.opacity = '1';
            menu.style.maxHeight = '500px';
        }, 10);
    }
}


window.addEventListener('click', function (event) {
    const menu = document.getElementById('dropdownMenu');
    const button = document.querySelector('.profile-dropdown-btn');


    if (!button.contains(event.target) && !menu.contains(event.target)) {
        menu.style.opacity = '0';
        menu.style.maxHeight = '0';
    }
});

function toggleAccessibilityMenu() {
    const menu = document.getElementById("accessibilityMenu");
    menu.style.display = menu.style.display === "flex" ? "none" : "flex";
}

function increaseFont() {
    document.body.style.fontSize = "larger";
}

function decreaseFont() {
    document.body.style.fontSize = "smaller";
}

let isHighContrast = false;
function toggleContrast() {
    document.body.classList.toggle("high-contrast");
    isHighContrast = !isHighContrast;
}

let isFocusMode = false;
function toggleFocusMode() {
    if (isFocusMode) {
        document.body.style.filter = "none";
    } else {
        document.body.style.filter = "grayscale(100%) brightness(1.1)";
    }
    isFocusMode = !isFocusMode;
}
