document.addEventListener('DOMContentLoaded', () => {
    const links = document.querySelectorAll('.settings-menu-link');
    const sections = document.querySelectorAll('.settings-section');

    function showSection(targetId) {
        sections.forEach(section => {
            section.style.display = 'none';
        });
        const targetSection = document.getElementById(targetId);
        if (targetSection) {
            targetSection.style.display = 'block';
        }
    }

    links.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();

            // Remove 'active' de todos os links
            links.forEach(l => l.parentElement.classList.remove('active'));

            // Adiciona 'active' ao link clicado
            link.parentElement.classList.add('active');

            const sectionId = link.getAttribute('data-section');
            showSection(sectionId);
        });
    });

    // Mostrar a seção 'seguranca' por padrão ao carregar a página
    showSection('seguranca');
});