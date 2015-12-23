%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

%   CSEE 6893: Big Data Analytics
%   Bridge Health Monitoring
%   Author: Karl S. Bayer (karlsbayer at gmail dot com)
%   Created: 12/2015
  
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%


%%
% find peak to peak of each frequency output
state1 = [acc(1,2:11:end); acc(1,3:11:end); acc(1,4:11:end); acc(1,5:11:end); acc(1,6:11:end); acc(1,7:11:end); acc(1,8:11:end); acc(1,9:11:end); acc(1,10:11:end); acc(1,11:11:end); acc(1,12:11:end)];
[f,yy] = fftDataMag(linspace(0,20,len),AnalysisresultsForKarl1(:,8));
%plot the frequency vs the pk2pk result
%compute euclidean distance between every combination

%adam suggests that we should take a spectrogram of the data over time.
findpeaks(abs(yy),f,'minpeakheight',max(abs(yy))/2)



%%
% sets up 2d matrix with different joints by row and 
% individual time values in columns.

load('acc.mat')
state1 = [acc(1,2:11:end); acc(1,3:11:end); acc(1,4:11:end); 
    acc(1,5:11:end); acc(1,6:11:end); acc(1,7:11:end); 
    acc(1,8:11:end); acc(1,9:11:end); acc(1,10:11:end); 
    acc(1,11:11:end); acc(1,12:11:end)];
len = length(state1);
%[f,yy] = fftDataMag(linspace(0,20,len),state1(1,:));
[f_s1,yy_s1] = fftDataMag(linspace(0,20,len),state1(:,:),2);

Y = abs(yy_s1);
X = repmat(f_s1,11,1);
name = repmat('NPeaks',11,1);
value = repmat(10,11,1);
rowparams=table(Y, X, name, value);
%[PKS_s1,LOCS_s1] = findpeaks(mag_s1, [f_s1; f_s1], 'NPeaks',10)

pks = rowfun( @findpeaks, rowparams,'OutputVariableNames',{'PKS_s1' 'LOCS_s1'});

% PKS_s1 = []
% LOCS_s1 = []
% for j=1:2
%     [PKS, LOCS] = findpeaks(Y(j,:),X(j,:), 'NPeaks',10)
%     PKS_s1 = vertcat(PKS_s1,PKS)
%     LOCS_s1 = vertcat(LOCS_s1,PKS)
% end

peaks = []
locs = []
for i=1:height(pks)
    peaks = horzcat(peaks, pks{i,1});
    locs = horzcat(locs, pks{i,2});
end


%% 
% fft data!! Main loops for computing similarity plots
num = 100;
ppeaks = [];
llocs = [];
YY = [];
freqs = [];
for i=1:num
    [peaks, locs, Y, freq] = fftpeaks(ZAccelerationForEveryId1000,i);
    ppeaks = vertcat(ppeaks, peaks);
    llocs = vertcat(llocs, locs);
    YY(:,:,i) = Y;
    freqs(:,:,i) = freq;
end

distances_fft = []
for j=1:num
    for k=1:num
        dist = 0;
        for l=1:size(YY,1)
            dist = dist + my_dist(YY(l,:,j), YY(l,:,k));
        end
        distances_fft(j,k) = dist;
    end
end

%%
% display plots for similarity data
figure('defaulttextfontsize', 16); plot(freqs(:,:,1).',YY(:,:,1).');
xlabel('Frequency (Hz)', 'FontSize', 16 )
ylabel('Magnitude (m/s2)', 'FontSize', 16 )
title('Frequency Responce of a Bridge State', 'FontSize', 20 )
legend('Sensor 1','Sensor 2','Sensor 3','Sensor...')

%%
figure; surf(distances_fft,'EdgeColor','none') %/max(max(distances_fft)))
xlabel('State #', 'FontSize', 16 )
ylabel('State #', 'FontSize', 16 )
title('Similarity of States (fft of accn.)', 'FontSize', 18 )
view(2)
c = colorbar
ylabel(c, 'Euclidean Norm of Residual', 'FontSize', 16)


%%
% similarity using acceleration data
num = 100;

acc = ZAccelerationForEveryId1000;
states = [];
srs = 8;
for i=1:num
    state1 = [  acc(i,2:srs:end); acc(i,3:srs:end); acc(i,4:srs:end); 
            acc(i,5:srs:end); acc(i,6:srs:end); acc(i,7:srs:end); 
            acc(i,8:srs:end); acc(i,9:srs:end); ];
    states(:,:,i) = state1;
end

% my_dist = @(x,y) sum((x-y).^2).^.5;

distances_acc = []
for j=1:num 
    for k=1:num
        dist = 0;
        for l=1:size(states,1)
            dist = dist + my_dist(states(l,:,j), states(l,:,k));
        end
        distances_acc(j,k) = dist;
    end
end

%%
figure; surf(distances_acc)%/max(max(distances_acc)))
xlabel('State #', 'FontSize', 16 )
ylabel('State #', 'FontSize', 16 )
title('Similarity of States (raw accn.)', 'FontSize', 18 )
view(2)
c = colorbar
ylabel(c, 'Euclidean Norm of Residual', 'FontSize', 16)

%%
distances_diff = abs(distances_fft/max(max(distances_fft)) - distances_acc/max(max(distances_acc)));
figure
surf(distances_diff)

%%
% distance function
my_dist = @(x,y) sum((x-y).^2).^.5;
%my_dist = @(x,y) pdist([x;y], 'chebychev')

%Sum of Euclidean Norm of the residuals
%residual of any 2 states
%euclidean norm of those residuals, 
%finally, sum the norms for each node




%% candidates
% provide a acc or fft value set, return a ranked list of
% possible similar states of the bridge.


%% Plot of bit data 2 states
st1node1 = acc(1,3:8:end);
st2node1 = acc(10,3:8:end);

figure; hold on;
subplot(2,1,1)
plot([1:length(st1node1)],st1node1,[1:length(st2node1)],st2node1)
xlabel('Time (s)','FontSize',16)
ylabel('Acceleration (m/s^2)','FontSize',16)
title('Responce of 2 Bridge States','FontSize',18)
legend('State 1, Node 2', 'State 10, Node 2');

dist = my_dist(st1node1, st2node1);
text(5, 5, sprintf('Distance: %d',dist))%,'FontSize',16)

%%
% individual plots
st1node1 = acc(1,3:8:end);
st2node1 = acc(800,3:8:end);
%my_createfigure([[1:length(st1node1)],[1:length(st2node1)]],[st1node1,st2node1]);
figure;
% subplot(2,1,2)
plot([1:length(st1node1)],st1node1,[1:length(st2node1)],st2node1)
% plot(st2node1)
xlabel('Time (s)','FontSize',16)
ylabel('Acceleration (m/s^2)','FontSize',16)
title('Responce of 2 Bridge States','FontSize',18)
legend('State 1, Node 2', 'State 800, Node 2');

dist = my_dist(st1node1, st2node1);
text(5, -26000, sprintf('Distance: %d',dist))%,'FontSize',16)

%% frequency plot
% load('TestInputs.mat')
figure('defaulttextfontsize', 16); hold on;
[peaks, locs, Y1, freq1] = fftpeaks(ZAccelerationForEveryId1000,1);
plot(freq1(2,:,1),Y1(2,:,1));
[peaks, locs, Y2, freq2] = fftpeaks(ZAccelerationForEveryId1000,800);
plot(freq2(2,:,1),Y2(2,:,1));
xlabel('Frequency (Hz)', 'FontSize', 16 )
ylabel('Magnitude (mm/s2)', 'FontSize', 16 )
title('Frequency Similarity of 2 Bridge States', 'FontSize', 20 )
legend('State 1', 'State 800')


%% plot of Our Contunous TestInputs
load('TestInputs.mat')
plot(state1(:,1),state1(:,2:end))
xlabel('Time (s)','FontSize',16)
ylabel('Acceleration (m/s^2)','FontSize',16)
title('Constant Stimulus Bridge Output, 30cm Deck','FontSize',18)
legend('.5 Hz','1 Hz','2 Hz','5 Hz','10 Hz','20 Hz');
