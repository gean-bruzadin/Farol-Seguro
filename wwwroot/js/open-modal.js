
const modal = document.getElementById("user-modal");
const openModalBtn = document.getElementById("openUserModalBtn");
const closeModalBtn = document.getElementById("closeUserModalBtn");
const editProfileBtn = document.getElementById("editProfileBtn");


openModalBtn.onclick = function () {
    modal.style.display = "block";
}


closeModalBtn.onclick = function () {
    modal.style.display = "none";
}


editProfileBtn.onclick = () => {
    window.location.href = '@Url.Action("Criar", "Usuário")';
};

const Quizmodal = document.getElementById("quiz-modal");
const openQuizModalBtn = document.getElementById("openQuizModalBtn");
const closeQuizModalBtn = document.getElementById("closeQuizModalBtn");


openQuizModalBtn.onclick = function () {
    Quizmodal.style.display = "block";
}


closeQuizModalBtn.onclick = function () {
    Quizmodal.style.display = "none";
}